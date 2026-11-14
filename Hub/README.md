# SecondMonitor Hub (Mac, Linux, Windows)

L'écran d'ingénieur de course de SecondMonitor. Il tourne sur l'ordinateur **qui ne joue pas** (par exemple un Mac) et s'affiche dans un navigateur : le Mac, un iPad, un téléphone…

```
PC Windows (joue)                         Mac (hub)
SecondMonitor + plugin                    SecondMonitor Hub
« Data Broadcast Server »  ── réseau ──▶  classement, carte, stratégies,
(tous les connecteurs de jeux)            IA (Ollama), interface web
```

Le PC de jeu ne fait que diffuser les données, comme avant. Tous les calculs se font sur le hub.

## Démarrage rapide (Mac)

1. Double-cliquez sur **`Lancer SecondMonitor Hub.command`**. Au premier lancement, il installe .NET si besoin, construit l'interface, puis ouvre le navigateur sur `http://localhost:8686`.
2. Sur le PC de jeu, lancez **SecondMonitor-Collecteur** (dossier `Collector/dist`, voir `Collector/LISEZ-MOI.md` ; Windows, Linux et SteamOS).
   Il est fait pour charger le moins possible le PC de jeu. Le SecondMonitor complet avec le plugin **Data Broadcast Server** (port 52642) fonctionne aussi.
3. Lancez une session dans le jeu. Le hub trouve le PC tout seul.
   - Si le PC et le Mac ne sont pas sur le même réseau (Ethernet ≠ Wi-Fi, autre sous-réseau, Tailscale) : *Réglages* → indiquez l'IP du PC.

Pour essayer sans jeu : *Réglages* → **Lancer la démo**. On peut aussi lancer `dotnet run --project src/SecondMonitor.Hub -- --demo`.

Pour ouvrir l'écran sur un autre appareil, utilisez l'adresse affichée dans *Réglages* (par exemple `http://192.168.1.30:8686`). Au premier lancement, macOS peut demander d'autoriser les connexions entrantes : acceptez.

## Ce qu'affiche le hub

- **Course** : classement complet avec l'écart au leader, à la catégorie ou à vous, l'intervalle, les tours, les pneus (gomme et tours dans le relais), les arrêts, le prochain arrêt estimé et le rythme avec la dégradation. Il y a aussi la carte de piste, apprise automatiquement en un tour, et votre stratégie.
- **Stratégies** : la frise des relais de tous les pilotes (gommes, arrêts, projection du prochain arrêt), et la liste des prochains arrêts attendus.
- **Ma voiture** : carburant, énergie virtuelle, usure et températures des pneus, pressions, arrêt conseillé.
- Un clic sur un pilote ouvre son graphique des temps au tour, ses relais et tous ses tours.

### Ce qui est mesuré et ce qui est estimé

Les écarts sont recalculés par le hub à partir des positions sur la piste, donc de la même façon quel que soit le jeu.

Les jeux ne transmettent généralement **ni le carburant ni l'usure des adversaires**. Leurs arrêts sont donc *estimés*, avec un indicateur de confiance (●●● / ●●○ / ●○○) :
- ●●● : d'après la longueur de leurs propres relais ;
- ●●○ : d'après les relais typiques de leur catégorie ;
- ●○○ : d'après l'autonomie de votre propre voiture.

## Ingénieur IA (Ollama, gratuit, 100 % local)

Onglet **Ingénieur IA** : on maintient le bouton (ou la barre d'espace) pour parler, ou on écrit une question. La réponse s'affiche et se fait entendre sur tous les écrans où « Écouter sur cet écran » est coché.

- Ollama tourne sur le **Mac**, jamais sur le PC de jeu.
- On choisit un modèle par rôle : **réflexion** (répond), **écoute** (transcrit la voix), **voix** (parle), ainsi que la voix elle-même. Un modèle omni comme `lfm3-omni:2b` peut tenir les trois rôles.
- **Fiabilité.** Les petits modèles calculent mal. Le hub calcule donc tous les chiffres et prépare une réponse pour les questions courantes : stand, essence, énergie, pneus, écarts, stratégie des voisins. Un garde-fou écarte toute réponse contenant un chiffre absent des données. Un clic sur « faits utilisés » montre exactement ce que l'IA a reçu.
- Le micro du navigateur ne fonctionne que sur `http://localhost` (ou en HTTPS). Depuis un iPad ou un téléphone, utilisez la saisie texte pour l'instant.
- **Écoute.** La voix part toujours en WAV 16 kHz mono 16 bits, sans les silences, découpée sur un silence au-delà de 28 s (réglable, carte « Lexique de piste ») ; un morceau refusé est renvoyé en morceaux de 10 s. Les autres formats sont convertis par ffmpeg (ou afconvert sur Mac). Le vocabulaire de piste est envoyé comme indice (`prompt`) aux modèles qui l'acceptent.
- **Lexique de piste.** `RadioLexicon` corrige les erreurs connues de la dictée (« books » → box…) et celles ajoutées par le pilote, et traduit les mots de piste anglais (brake bias, stint, delta, fuel…) pour les règles du hub.

## Musique Deezer

Carte **Musique** (Réglages) et commandes vocales sans l'IA. La musique joue sur l'ordinateur de SecondMonitor, ou sur le **PC de jeu** (réglage « Musique sur », case « Musique Deezer sur ce PC » du collecteur, Windows) : la recherche et les commandes restent sur l'ordinateur, la lecture se fait dans l'application Deezer du PC, dans le casque, et s'efface quand l'ingénieur parle. L'étoile d'un résultat de recherche choisit la **playlist préférée** (« Lecture », « mets ma playlist », lancement automatique au début de chaque session si coché).

## Construire pour tous les systèmes

Depuis le Mac, sans machine Windows ou Linux : `./construire.sh tous` à la racine du projet, ou `mac`, `win`, `linux`.
Les archives arrivent dans `dist/` : le hub pour Mac (Apple Silicon et Intel), Windows et Linux, et le collecteur pour Windows et Linux/SteamOS.
Dans **OneDrop3D** (Publieur), le produit **SecondMonitor** lance ces mêmes commandes. La version est dans `Hub/Directory.Build.props`,
`Collector/Directory.Build.props` et `Hub/web/package.json`, et OneDrop3D la change dans les trois fichiers.

## Développement

```
Hub/
  src/SecondMonitor.Hub.Core   moteur multiplateforme : réseau (LiteNetLib/LZ4/protobuf, identique à Windows),
                               course, stratégie, carte, démo. Réutilise par liens les sources de Foundation/DataModel.
  src/SecondMonitor.Hub        serveur ASP.NET Core : API, WebSocket /ws, interface web (wwwroot)
  tests/SecondMonitor.Hub.Tests tests (protocole de bout en bout, moteur de course)
  web/                         interface React + TypeScript (Vite)
```

- Tests : `dotnet test Hub/SecondMonitorHub.sln`
- Interface en développement : `cd Hub/web && npm run dev` (proxy vers le hub sur le port 8686)
- Construction de l'interface : `npm run build` (sortie dans `src/SecondMonitor.Hub/wwwroot`)

Les réglages et les cartes apprises sont dans `~/Library/Application Support/SecondMonitorHub` (variable `SMHUB_DATA` pour en changer).

Seule modification de l'arbre Windows d'origine : `Foundation/DataModel/BasicProperties/ColorDTO.cs` a été rendu portable (`SM_PORTABLE`), avec des valeurs de couleur strictement identiques.
