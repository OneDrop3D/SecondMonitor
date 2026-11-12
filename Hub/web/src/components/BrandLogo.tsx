import {
  siAcura, siAlfaromeo, siAstonmartin, siAudi, siBentley, siBmw, siBugatti, siCadillac, siChevrolet, siCitroen, siFerrari, siFord,
  siHonda, siHyundai, siInfiniti, siJaguar, siKia, siKoenigsegg, siLamborghini, siMaserati, siMazda, siMclaren, siMercedes, siMg,
  siMini, siMitsubishi, siNissan, siOpel, siPeugeot, siPorsche, siRenault, siSeat, siSkoda, siSubaru, siTesla, siToyota, siVolkswagen, siVolvo,
  type SimpleIcon,
} from 'simple-icons';

/** Logos des marques (simple-icons, CC0) reconnues dans le nom de la voiture ou de l'équipe. */
const BRANDS: { re: RegExp; icon?: SimpleIcon; name: string }[] = [
  { re: /porsche/i, icon: siPorsche, name: 'Porsche' },
  { re: /ferrari/i, icon: siFerrari, name: 'Ferrari' },
  { re: /\bbmw\b/i, icon: siBmw, name: 'BMW' },
  { re: /\baudi\b|\br8\b/i, icon: siAudi, name: 'Audi' },
  { re: /mercedes|\bamg\b/i, icon: siMercedes, name: 'Mercedes' },
  { re: /toyota|\bgr0\d\d|gr010|\bgr86\b|\bsupra\b/i, icon: siToyota, name: 'Toyota' },
  { re: /peugeot|9x8/i, icon: siPeugeot, name: 'Peugeot' },
  { re: /cadillac|v-series/i, icon: siCadillac, name: 'Cadillac' },
  { re: /lamborghini|hurac[aá]n|sc63/i, icon: siLamborghini, name: 'Lamborghini' },
  { re: /mclaren|\b720s\b|\b570s\b|artura/i, icon: siMclaren, name: 'McLaren' },
  { re: /aston\s*martin|vantage|valkyrie/i, icon: siAstonmartin, name: 'Aston Martin' },
  { re: /\bford\b|mustang|\bgt40\b/i, icon: siFord, name: 'Ford' },
  { re: /chevrolet|corvette|camaro/i, icon: siChevrolet, name: 'Chevrolet' },
  { re: /\bhonda\b|\bnsx\b/i, icon: siHonda, name: 'Honda' },
  { re: /\bacura\b|\barx-?\d/i, icon: siAcura, name: 'Acura' },
  { re: /nissan|\bgt-?r\b/i, icon: siNissan, name: 'Nissan' },
  { re: /bentley/i, icon: siBentley, name: 'Bentley' },
  { re: /maserati/i, icon: siMaserati, name: 'Maserati' },
  { re: /alfa\s*romeo/i, icon: siAlfaromeo, name: 'Alfa Romeo' },
  { re: /hyundai/i, icon: siHyundai, name: 'Hyundai' },
  { re: /renault/i, icon: siRenault, name: 'Renault' },
  { re: /volkswagen|\bvw\b|golf/i, icon: siVolkswagen, name: 'Volkswagen' },
  { re: /\bopel\b/i, icon: siOpel, name: 'Opel' },
  { re: /mazda/i, icon: siMazda, name: 'Mazda' },
  { re: /subaru/i, icon: siSubaru, name: 'Subaru' },
  { re: /\bkia\b/i, icon: siKia, name: 'Kia' },
  { re: /citro[eë]n/i, icon: siCitroen, name: 'Citroën' },
  { re: /tesla/i, icon: siTesla, name: 'Tesla' },
  { re: /\bseat\b|cupra/i, icon: siSeat, name: 'Seat' },
  { re: /skoda/i, icon: siSkoda, name: 'Škoda' },
  { re: /jaguar/i, icon: siJaguar, name: 'Jaguar' },
  { re: /\bmini\b/i, icon: siMini, name: 'Mini' },
  { re: /volvo/i, icon: siVolvo, name: 'Volvo' },
  { re: /mitsubishi/i, icon: siMitsubishi, name: 'Mitsubishi' },
  { re: /infiniti/i, icon: siInfiniti, name: 'Infiniti' },
  { re: /bugatti/i, icon: siBugatti, name: 'Bugatti' },
  { re: /koenigsegg/i, icon: siKoenigsegg, name: 'Koenigsegg' },
  { re: /\bmg\b/i, icon: siMg, name: 'MG' },
  // Marques sans logo libre : monogramme.
  { re: /alpine|a424|a110/i, name: 'Alpine' },
  { re: /lexus|rc ?f/i, name: 'Lexus' },
  { re: /oreca/i, name: 'Oreca' },
  { re: /ligier/i, name: 'Ligier' },
  { re: /isotta/i, name: 'Isotta Fraschini' },
  { re: /glickenhaus/i, name: 'Glickenhaus' },
  { re: /vanwall/i, name: 'Vanwall' },
  { re: /dallara/i, name: 'Dallara' },
  { re: /ginetta/i, name: 'Ginetta' },
];

export function findBrand(...texts: (string | null | undefined)[]) {
  const t = texts.filter(Boolean).join(' ');
  return t ? BRANDS.find((b) => b.re.test(t)) ?? null : null;
}

/** Logo de la marque (monochrome, lisible en thème clair comme sombre) ou monogramme si la marque n'a pas de logo libre. */
export function BrandLogo({ car, team, size = 18 }: { car: string | null | undefined; team?: string | null; size?: number }) {
  const brand = findBrand(car, team);
  if (!brand) return <span className="brand-logo empty" style={{ width: size, height: size }} aria-hidden />;
  if (!brand.icon) {
    return (
      <span className="brand-logo mono" style={{ width: size, height: size, fontSize: size * 0.5 }} title={brand.name} aria-label={brand.name}>
        {brand.name.split(/\s+/).map((w) => w[0]).join('').slice(0, 2).toUpperCase()}
      </span>
    );
  }
  return (
    <svg className="brand-logo" width={size} height={size} viewBox="0 0 24 24" role="img" aria-label={brand.name}>
      <title>{brand.name}</title>
      <path d={brand.icon.path} fill="currentColor" />
    </svg>
  );
}
