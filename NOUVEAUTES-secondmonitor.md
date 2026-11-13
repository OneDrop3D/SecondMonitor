<!--
  NOUVEAUTÉS À PUBLIER — fichier lu par le Publieur OneDrop3D.
  POUR L'IA DE CODE : quand tu termines des modifications sur cette application, ajoute ici,
  sous ce commentaire, une ligne par point visible par l'utilisateur (Markdown, en français,
  ex. « - **Réglages** : le thème sombre s'applique aussi à la fenêtre d'aide »).
  Mets aussi à jour le README / la documentation si une fonction change.
  Ne mets pas de numéro de version : le Publieur le gère. Il vide ce fichier après la publication.
-->
- **Windows** : SecondMonitor s'affiche de nouveau dans sa fenêtre (elle pouvait rester noire quand le collecteur avait été ouvert sur le même PC).
- **Fenêtre** : écran « Chargement de SecondMonitor… » au démarrage ; si l'interface ne s'affiche pas, elle s'ouvre toute seule dans le navigateur et la raison est notée dans le journal. Option `--sans-gpu` (ou « fenetreSansGpu » dans settings.json) pour les pilotes graphiques capricieux.
- **Collecteur** : nouvelle carte « Radio de l'ingénieur » : la voix de l'ingénieur peut sortir sur le PC de jeu, dans le casque choisi (bouton « Tester »).
- **Collecteur** : « Parler avec le micro de ce PC » : le bouton radio du PC de jeu (et ClickDeck) enregistre le micro choisi sur le PC de jeu, avec un test du niveau du micro ; un appui pendant que l'ingénieur parle le coupe.
- **Réglages** : nouvelle carte « Voix de l'ingénieur » : voix sur le PC de jeu, sur les écrans de SecondMonitor, ou les deux ; la carte Radio de l'Ingénieur IA indique quand le PC de jeu a la voix ou le micro.
- **Ingénieur IA, alertes de pilotage immédiates** : limites de piste (« Track limits virage 5 : trop large. 2 sur 3 : la prochaine, c'est la pénalité »), coupé (« levez un peu le pied »), temps annulé, pénalité reçue, moteur coupé ou point mort au départ. Réglage « Alertes de pilotage immédiates ».
- **Ingénieur IA, usure brutale analysée** : quand un pneu prend plusieurs points d'usure en quelques secondes, l'ingénieur dit où (virage), dans quelle phase (freinage, entrée, milieu, sortie), la cause probable et le réglage à essayer avec les valeurs actuelles (« TC de 4 à 5 », « répartition de 54,5 à 54,0 % »), puis demande ce qui s'est passé.
- **Ingénieur IA, réglages de la voiture** : répartition, migration, TC (niveau, coupure, glissement), ABS, cartographie, regen, moteur électrique, batterie et énergie virtuelle lus dans le jeu (Le Mans Ultimate ; F1 24 et ACC en partie). « TC ? », « la carto ? », « la balance des freins » répondent avec la valeur exacte.
- **Ingénieur IA, catégories de voitures** : Hypercar LMH (hybride sur l'avant au-dessus de 190 km/h) et LMDh (hybride sur l'arrière), LMP2, LMGT3, GTE, GT3, GT4, F1 : ce qui se règle en roulant, ce que fait chaque réglage, et jamais un réglage que la voiture n'a pas.
- **Ingénieur IA, réponses courtes** : les températures et l'usure aux quatre roues, sans commentaire (« Avant gauche 80 degrés, avant droit 90, arrière gauche 50, arrière droit 55 ») ; les mots seuls (« Pneus », « TC », « Fure des pneus ») sont compris ; plus de « Vous êtes P1 » quand vous roulez seul en essais (comparaison avec vos propres meilleurs micro-secteurs).
- **Ingénieur IA, « Copy »** : accusés de réception en anglais comme une vraie radio d'équipe (réglable). Si l'IA réfléchit plus de 2,5 s : « Copy, je regarde ça et je reviens vers vous », vous pouvez poser d'autres questions, et la réponse arrive ensuite (« Pour la stratégie : … »). Les questions qui demandent l'IA passent dans l'ordre, une à la fois.
- **Ingénieur IA, spécialistes** : pneus, stratégie, voiture, course ; chacun ne reçoit que les faits de son domaine (réponse plus rapide, deux phrases au plus) et peut avoir son propre modèle (carte « Spécialistes »).
- **Ingénieur IA, demandes d'arrêt** : « je boxe », « je veux des softs neufs », « la réparation aussi », « fais le plein » sont confirmés et gardés pour l'arrêt (écran et mémoire de course).
- **Le Mans Ultimate, menu des stands réglé par l'ingénieur** (expérimental) : à la première demande, l'ingénieur propose « Je peux remplir le menu des stands moi-même. Je l'active ? » ; ensuite il règle pneus, réparation et énergie ou carburant par le collecteur du PC de jeu (API locale du jeu), vérifie et le dit à la radio. « Active / désactive le menu des stands », « essaie l'API » (lecture seule) ; réglage dans l'écran Ingénieur IA et case dans le collecteur.
- **Ingénieur IA, tutoiement** : l'ingénieur te tutoie à la radio, comme une vraie équipe.
- **Limites de piste désactivables à la voix** : « coupe les limites de piste », « remets les limites de piste » (et « coupe les alertes » pour toutes les alertes de pilotage), aussi dans l'écran Ingénieur IA.
- **Voix** : une vraie pause entre les valeurs d'une liste (les virgules n'étaient pas marquées à l'oral).
- **Pédales et volant** : nouveau bloc de l'accueil avec l'accélérateur, le frein et le volant en direct et sur tout le tour, comparés à votre meilleur tour ; l'ingénieur analyse votre pilotage (« Peux-tu analyser mon pilotage ? »).
- **Réglages et limites** : nouveau bloc de l'accueil (répartition, TC, ABS, cartographie, énergie, points de limites de piste, pénalités).
- **Sessions** : boutons « Terminer la session » et « Nouvelle session », et menu déroulant pour revoir les échanges d'une session passée (écran Ingénieur IA).
- **Dégâts** : plus de fausses « roues arrachées » en entrant au garage ou en sortant des stands.
