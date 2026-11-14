namespace SecondMonitor.Hub.Core.Ai
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;

    /// <summary>
    /// Préparation de la voix du pilote pour le modèle « écoute » : tout WAV (8, 16, 24, 32 bits, flottant, stéréo, 44,1 ou 48 kHz…)
    /// est converti dans le format que tous les modèles acceptent — WAV PCM 16 bits, mono, 16 kHz —, les silences du début et
    /// de la fin sont retirés, et un enregistrement trop long est découpé sur un silence (certains modèles refusent plus de 30
    /// ou 60 secondes, d'autres se mettent à inventer).
    /// </summary>
    public static class WavAudio
    {
        public const int TargetRate = 16000;

        /// <summary>Le fichier commence-t-il comme un WAV (RIFF … WAVE) ?</summary>
        public static bool IsWav(byte[] data) =>
            data != null && data.Length >= 12 && Encoding.ASCII.GetString(data, 0, 4) == "RIFF" && Encoding.ASCII.GetString(data, 8, 4) == "WAVE";

        /// <summary>Échantillons mono (-1..1) et fréquence d'un WAV. Null si le format n'est pas lisible.</summary>
        public static (float[] Samples, int Rate)? Decode(byte[] wav)
        {
            if (!IsWav(wav))
            {
                return null;
            }

            int format = 0, channels = 0, rate = 0, bits = 0;
            int dataStart = -1, dataLength = 0;
            int pos = 12;
            while (pos + 8 <= wav.Length)
            {
                string id = Encoding.ASCII.GetString(wav, pos, 4);
                int size = BitConverter.ToInt32(wav, pos + 4);
                int body = pos + 8;
                if (id == "fmt " && body + 16 <= wav.Length)
                {
                    format = BitConverter.ToUInt16(wav, body);
                    channels = BitConverter.ToUInt16(wav, body + 2);
                    rate = BitConverter.ToInt32(wav, body + 4);
                    bits = BitConverter.ToUInt16(wav, body + 14);
                    if (format == 0xFFFE && size >= 40 && body + 26 <= wav.Length)
                    {
                        format = BitConverter.ToUInt16(wav, body + 24); // WAVE_FORMAT_EXTENSIBLE : le vrai format est dans le sous-format
                    }
                }
                else if (id == "data")
                {
                    dataStart = body;
                    // Taille inconnue (enregistrement en flux) ou fausse : jusqu'à la fin du fichier.
                    dataLength = size <= 0 || body + (long)size > wav.Length ? wav.Length - body : size;
                    break;
                }

                if (size < 0 || body + (long)size > wav.Length)
                {
                    break;
                }

                pos = body + size + (size & 1);
            }

            int bytes = bits / 8;
            if (dataStart < 0 || channels <= 0 || rate <= 0 || bytes <= 0 || (format != 1 && format != 3))
            {
                return null;
            }

            int frame = bytes * channels;
            int frames = dataLength / frame;
            var mono = new float[frames];
            for (int i = 0; i < frames; i++)
            {
                double sum = 0;
                int o = dataStart + (i * frame);
                for (int c = 0; c < channels; c++)
                {
                    sum += Sample(wav, o + (c * bytes), bytes, format == 3);
                }

                mono[i] = (float)(sum / channels);
            }

            return (mono, rate);
        }

        /// <summary>Change la fréquence (filtre simple contre le repliement quand on descend, puis interpolation linéaire).</summary>
        public static float[] Resample(float[] samples, int from, int to)
        {
            if (samples == null || samples.Length == 0 || from == to || from <= 0 || to <= 0)
            {
                return samples ?? Array.Empty<float>();
            }

            float[] source = samples;
            if (to < from)
            {
                int width = Math.Max(1, (int)Math.Round((double)from / to));
                if (width > 1)
                {
                    source = new float[samples.Length];
                    double acc = 0;
                    for (int i = 0; i < samples.Length; i++)
                    {
                        acc += samples[i];
                        if (i >= width)
                        {
                            acc -= samples[i - width];
                        }

                        source[i] = (float)(acc / Math.Min(i + 1, width));
                    }
                }
            }

            int length = (int)Math.Max(1, (long)samples.Length * to / from);
            var result = new float[length];
            double step = (double)from / to;
            for (int i = 0; i < length; i++)
            {
                double x = i * step;
                int a = Math.Min((int)x, source.Length - 1);
                int b = Math.Min(a + 1, source.Length - 1);
                double f = x - a;
                result[i] = (float)((source[a] * (1 - f)) + (source[b] * f));
            }

            return result;
        }

        /// <summary>WAV PCM 16 bits mono.</summary>
        public static byte[] EncodePcm16(float[] samples, int rate)
        {
            samples ??= Array.Empty<float>();
            using var ms = new MemoryStream(44 + (samples.Length * 2));
            using var w = new BinaryWriter(ms);
            w.Write(Encoding.ASCII.GetBytes("RIFF"));
            w.Write(36 + (samples.Length * 2));
            w.Write(Encoding.ASCII.GetBytes("WAVEfmt "));
            w.Write(16);
            w.Write((short)1);
            w.Write((short)1);
            w.Write(rate);
            w.Write(rate * 2);
            w.Write((short)2);
            w.Write((short)16);
            w.Write(Encoding.ASCII.GetBytes("data"));
            w.Write(samples.Length * 2);
            foreach (float s in samples)
            {
                float c = Math.Clamp(s, -1f, 1f);
                w.Write((short)(c < 0 ? c * 32768 : c * 32767));
            }

            w.Flush();
            return ms.ToArray();
        }

        /// <summary>
        /// Prépare un WAV pour la transcription : 16 kHz mono, silences du début et de la fin retirés, découpé en morceaux d'au plus
        /// <paramref name="maxSeconds"/> secondes (coupés sur le passage le plus calme). Liste vide si l'enregistrement n'est que silence ;
        /// null si le fichier n'est pas un WAV lisible (il part alors tel quel).
        /// </summary>
        public static List<byte[]> ForTranscription(byte[] wav, double maxSeconds)
        {
            var decoded = Decode(wav);
            if (decoded == null)
            {
                return null;
            }

            var (samples, rate) = decoded.Value;

            float[] mono = Resample(samples, rate, TargetRate);
            mono = TrimSilence(mono, TargetRate);
            var chunks = new List<byte[]>();
            if (mono.Length < TargetRate / 5)
            {
                return chunks; // moins de 0,2 s de voix : rien à transcrire
            }

            foreach (float[] part in Split(mono, TargetRate, maxSeconds))
            {
                chunks.Add(EncodePcm16(part, TargetRate));
            }

            return chunks;
        }

        /// <summary>Durée d'un WAV en secondes (0 si illisible).</summary>
        public static double Duration(byte[] wav) => Decode(wav) is { } d && d.Rate > 0 ? (double)d.Samples.Length / d.Rate : 0;

        /// <summary>Retire les silences du début et de la fin (on garde un quart de seconde de marge).</summary>
        public static float[] TrimSilence(float[] samples, int rate)
        {
            int window = Math.Max(1, rate / 50); // 20 ms
            int count = samples.Length / window;
            if (count == 0)
            {
                return samples;
            }

            var rms = new double[count];
            double peak = 0;
            for (int i = 0; i < count; i++)
            {
                double sum = 0;
                for (int j = 0; j < window; j++)
                {
                    float v = samples[(i * window) + j];
                    sum += v * v;
                }

                rms[i] = Math.Sqrt(sum / window);
                peak = Math.Max(peak, rms[i]);
            }

            double threshold = Math.Max(0.004, peak * 0.06);
            int first = Array.FindIndex(rms, r => r >= threshold);
            int last = Array.FindLastIndex(rms, r => r >= threshold);
            if (first < 0 || peak < 0.004)
            {
                return Array.Empty<float>();
            }

            int pad = rate / 4;
            int start = Math.Max(0, (first * window) - pad);
            int end = Math.Min(samples.Length, ((last + 1) * window) + pad);
            var result = new float[end - start];
            Array.Copy(samples, start, result, 0, result.Length);
            return result;
        }

        /// <summary>Découpe en morceaux d'au plus <paramref name="maxSeconds"/> s, chacun coupé sur le passage le plus calme de sa fin.</summary>
        public static List<float[]> Split(float[] samples, int rate, double maxSeconds)
        {
            var parts = new List<float[]>();
            int max = (int)(Math.Clamp(maxSeconds, 5, 600) * rate);
            int start = 0;
            while (samples.Length - start > max)
            {
                // Le passage de 300 ms le plus calme dans le dernier tiers de la fenêtre.
                int window = rate * 3 / 10;
                int from = start + (max * 2 / 3);
                int to = start + max - window;
                int cut = start + max;
                double best = double.MaxValue;
                for (int i = from; i <= to; i += rate / 50)
                {
                    double sum = 0;
                    for (int j = 0; j < window; j++)
                    {
                        float v = samples[i + j];
                        sum += v * v;
                    }

                    if (sum < best)
                    {
                        best = sum;
                        cut = i + (window / 2);
                    }
                }

                parts.Add(Slice(samples, start, cut));
                start = cut;
            }

            parts.Add(Slice(samples, start, samples.Length));
            return parts;
        }

        private static float[] Slice(float[] s, int start, int end)
        {
            var r = new float[Math.Max(0, end - start)];
            Array.Copy(s, start, r, 0, r.Length);
            return r;
        }

        private static double Sample(byte[] b, int o, int bytes, bool floating)
        {
            if (floating)
            {
                return bytes == 8 ? BitConverter.ToDouble(b, o) : BitConverter.ToSingle(b, o);
            }

            return bytes switch
            {
                1 => (b[o] - 128) / 128.0,
                2 => BitConverter.ToInt16(b, o) / 32768.0,
                3 => ((b[o] << 8) | (b[o + 1] << 16) | (b[o + 2] << 24)) / 2147483648.0,
                4 => BitConverter.ToInt32(b, o) / 2147483648.0,
                _ => 0,
            };
        }
    }
}
