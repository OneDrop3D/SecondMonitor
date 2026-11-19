# SecondMonitor (fork OneDrop3D) — journal des versions

SecondMonitor (l'application qui affiche) et SecondMonitor Collecteur (PC de jeu), pour Windows, Mac et Linux/SteamOS.
Basé sur [SecondMonitor](https://gitlab.com/winzarten/SecondMonitor) de winzarten (licence MIT).

## Version 0.7.7

- **Analyse de course** (nouvel écran, marche sans le PC de jeu) : choisissez une course enregistrée, puis des tours, un ou plusieurs relais ; affichez la moyenne par relais, la moyenne de tous les tours choisis, ou comparez jusqu'à 6 tours. Vitesse, écart cumulé, accélérateur et frein, rapports et régime le long du tour, avec un tableau temps moyen, Vmax, essence, énergie et usure de chaque pneu, puis le bilan par tour du carnet. Les tracés de chaque tour sont désormais gardés avec la course (les sessions plus anciennes n'ont que le bilan). Depuis l'écran d'attente, « Analyser une course passée » y mène directement.
- **Préparer la course** (nouvel écran, marche sans le PC de jeu) : durée ou nombre de tours, temps au tour, essence, énergie, réservoir, temps au stand et vie des pneus (repris en un clic des moyennes d'une course passée) donnent le plan d'arrêts relais par relais (tour d'arrêt, essence et énergie à remettre, ratio, pneus, temps immobile), et les variantes un arrêt de moins / de plus. « Garder ce plan pour la course » : l'ingénieur le connaît en course sur ce circuit et vise ses tours d'arrêt tant qu'ils restent dans la fenêtre ; ce que vous décidez à la radio passe avant.
- **Accélérateur en vert, frein en rouge** dans les graphiques de pédales (Mes tours comparés, Analyse de course) ; quand plusieurs tours sont tracés, chacun a son trait (plein, tirets, points).
- **Zoom et défilement dans tous les graphiques** : glissez sur un graphique pour agrandir une zone, Ctrl/⌘ + molette ou pincement du trackpad pour zoomer, barre sous le graphique (ou glissement à deux doigts) pour défiler, double-clic ou « Tout » pour revenir. Dans Mes tours comparés et Analyse de course, tous les graphiques suivent le même zoom ; la moyenne de chaque courbe sur la partie affichée s'écrit dessous.

---

## Version 0.7.6

- **Relais rallongé à la voix** : dites « on rallonge le relais de deux tours », « je veux faire deux tours de plus » ou « et si on fait trois tours de moins ? » ; l'ingénieur donne le nouveau tour d'arrêt, la consommation à viser par tour (énergie et essence), dit si c'est jouable (à une question, il demande « On le fait ? » : répondez « oui », « on le fait », « vas-y » ou « ok » pour décider), règle le menu des stands de LMU sur ce nouveau plan, indique où lever le pied (virages, distance avant le freinage, d'après votre essai de lift and coast ou en estimation) et vous dit à chaque tour si vous êtes dans la cible.
- **Carnet, bilan par tour** : pour une session passée, ouvrez les catégories Temps au tour, Essence, Énergie virtuelle et Pneus (usure de chaque roue) : la valeur de chaque tour, la moyenne de chaque relais (avec sa gomme) et celle de la course. Décochez un tour (accident, safety car…) pour le retirer des moyennes, recochez-le pour l'y remettre ; le choix est enregistré dans le carnet. Les tours au stand et les tours invalides hors du rythme sont retirés d'office. Les anciens carnets n'ont que les temps : l'essence, l'énergie et l'usure par tour sont enregistrées à partir de cette version.
- **Carte des zones de vitesse** : dans Vitesses & trajectoires › Zones et profils, une carte du circuit montre les lignes droites (vert, Vmax) et les virages (orange, Vmin) avec leur numéro ; survolez ou touchez une zone, sur la carte ou dans le tableau, pour la mettre en évidence des deux côtés et lire sa valeur pour les pilotes comparés (un clic la garde, et sa colonne vient au milieu du tableau). Au Mans, les zones portent leur nom : Dunlop, Esses de la Forêt, Tertre Rouge, Hunaudières et ses deux chicanes, Mulsanne, Indianapolis, Arnage, Virages Porsche, Chicane Ford.
- **Écart avec la voiture de devant** : en course, le bandeau du haut (sur tous les écrans, dont le Chronométrage) affiche la voiture devant vous dans votre catégorie, l'écart, et ce que vous reprenez ou perdez par tour (moyenne des 3 derniers tours).

---

## Version 0.7.5

- **Windows, fenêtre noire au lancement** : l'interface s'ouvre dans le navigateur au bout de 6 s au lieu de 15, et SecondMonitor retient de démarrer la fenêtre sans accélération graphique au lancement suivant (cause habituelle d'une fenêtre WebView2 noire).
- **Mises à jour automatiques** : avec « Vérifier automatiquement » coché, la nouvelle version se télécharge et s'installe toute seule (SecondMonitor redémarre), jamais pendant une session de jeu : elle attend que le jeu n'envoie plus rien depuis 2 minutes. Même chose pour le collecteur du PC de jeu. Une version mise de côté avec « Plus tard » ne s'installe pas toute seule.
- **PC de jeu relié par câble** (Ethernet, adaptateur USB-C vers Ethernet, ou câble USB4 / Thunderbolt quand le PC le permet) : SecondMonitor cherche maintenant le collecteur sur toutes les connexions du Mac, pas seulement la principale ; plus besoin de saisir l'adresse du câble. Nécessite le collecteur à jour sur le PC de jeu.

---

## Version 0.7.4

- **Reprise de la course après un redémarrage** : si tu fermes et relances SecondMonitor pendant la même session du jeu, les tours, la consommation au tour, les arrêts, les relais des adversaires, la mémoire de l'ingénieur et le carnet continuent là où ils s'étaient arrêtés. Réglages › « Reprendre la course après un redémarrage » pour le couper ; « Nouvelle session » efface la course enregistrée. Le carnet n'est plus coupé en deux au relancement.
- **Stratégie : nombre d'arrêts** : « combien d'arrêts ? », « il faudra faire encore deux arrêts ? », « un box ou deux ? », « je suis obligé de faire un quatrième arrêt ? » donnent oui / non, les arrêts restants, la fenêtre du prochain (au plus tôt, au plus tard) et ce qu'il faut économiser par tour pour en faire un de moins.
- **Stratégie : décaler l'arrêt** : « au tour 21 », « je boxe au tour d'après », « et si je boxe un tour plus tôt ? » : l'ingénieur dit si ça passe (ou à quel tour l'énergie est vide), combien d'arrêts au total, ce qu'on mettra et la durée de l'arrêt ; l'arrêt décidé est retenu.
- **Stratégie : ratio carburant / énergie** : « le ratio », « je suis à 0,97 » : ratio idéal calculé sur ta consommation (litres par % d'énergie), ce que tu emportes en trop ou en moins, la valeur à mettre.
- **Stratégie : « qu'est-ce qu'on peut jouer ? »**, « combien de tours avant la fin ? », « on peut finir avec ce plein ? », « c'est quoi l'énergie virtuelle ? » : réponses calculées, courtes.
- **Lift and coast** : « je fais un tour en lift and coast » (compris aussi « Lyssenko », « live and course »), « le tour 18 c'est ma référence ». À la fin du tour, l'ingénieur donne le temps perdu et l'énergie / l'essence économisées, demande si le tour était propre, puis dit si un arrêt de moins en lift and coast fait gagner ou perdre du temps. Si tu valides, il te donne la consommation cible et te prévient quand tu la dépasses. Il peut aussi te proposer l'essai de lui-même quand un arrêt de moins est à portée.
- **Batterie** : « batterie », « consommation de la batterie » donnent d'abord le pourcentage (plus l'essence ni l'énergie virtuelle), le bilan du dernier tour et la comparaison au même endroit du tour précédent ; réponse plus courte.
- **Plan de course à l'écran** : écran Stratégies et nouveau bloc d'accueil « Plan de course » (Disposition › Ajouter un bloc) : arrêts restants, tours à faire, autonomie, ratio idéal, arrêts prévus avec ce qu'on y met, un tour plus tôt / plus tard, un arrêt de moins et le lift and coast mesuré. Le bloc Stratégie affiche un résumé d'une ligne.
- **Moins bavard** : « l'écart à chaque passage de la ligne » donne une annonce par tour (au lieu d'une par seconde gagnée) ; plus de « pneus froids » à chaque sortie des stands en course ; « dernier tour » annoncé au début du dernier tour.
- **Corrections** : « box box » ne demande plus 108 % d'énergie (plafonné à ce qui rentre) ; « je ressors en quelle position » (même mal dicté) donne la position à la sortie des stands ; « où je perds du temps » compare à tes meilleurs passages quand tu es déjà le plus rapide de ta catégorie, et « par rapport à mon dernier tour » compare le tour en cours au précédent ; la consommation au tour ne compte plus le premier tour mesuré en partie quand l'application démarre en pleine course.
- **Le Mans Ultimate, ratio carburant / énergie dans le menu des stands** : « règle le ratio » (ou « mets le ratio à 0,94 ») le règle dans le menu des stands à la valeur conseillée d'après ta consommation ; avec « box », le ratio est réglé en même temps que l'énergie. « Comment je modifie mon ratio ? » explique où il se trouve dans le menu du jeu. Nécessite le collecteur à jour sur le PC de jeu.
- **Diagnostic du menu des stands sur le Mac** : chaque réglage ou test du menu des stands est maintenant recopié en détail (menu lu, changements, ce que le jeu a gardé ou refusé) dans `menu-stands-lmu.txt` du dossier de données de SecondMonitor, plus seulement sur le PC de jeu.

---

## Version 0.7.3

- **Ingénieur IA, moins bavard** : un conseil de batterie (attaque ou défense) et une annonce de duel toutes les 10 minutes au plus, quel que soit l'adversaire (il y en avait un par minute) ; une alerte d'usure anormale toutes les 4 minutes au plus, jamais tant que la précédente attend ta réponse, et plus aucune alerte après le drapeau à damier ni au stand.
- **Ingénieur IA, accusés de réception variés** : « Copy » une fois sur quatre environ, sinon « Reçu », « Compris » ou rien ; formules d'attente différentes (« Une seconde, je vérifie », « Je regarde et je te dis »).
- **Ingénieur IA, phrase mal entendue** : une phrase sans sujet de course ni question (dictée ratée) reçoit « Pas compris, tu peux répéter ? » tout de suite, au lieu de « je reviens vers toi » suivi d'une réponse à côté.
- **Temps perdu au stand** : « Combien de temps prend un arrêt ? », « combien je perds au stand ? » donnent le temps perdu (mesuré sur les arrêts vus, sinon estimé) ; après ton arrêt, l'ingénieur annonce ce qu'il t'a coûté.
- **« Box box »** : sans autre précision, l'ingénieur prend le plan calculé (« 3 L et 10 % d'énergie ») et règle le menu des stands de Le Mans Ultimate avec ; « des pneus parce que les miens ont des plats » est compris comme une demande de pneus neufs.
- **Écart annoncé** : « annonce-moi l'écart à chaque fois qu'il baisse d'une seconde » (ou « avec celui de derrière », « toutes les deux secondes ») ; « arrête les écarts » pour couper.
- **Modèle pour les questions difficiles** : carte Spécialistes, les questions d'analyse (« pourquoi… », plusieurs sujets) peuvent aller à un plus gros modèle, par exemple sur un autre ordinateur ; les questions simples restent rapides. Le modèle qui a répondu s'affiche sous la réponse.
- **Écoute** : la sortie brute de Qwen3-ASR (« language French<asr_text>… ») est nettoyée : les commandes en début de phrase sont de nouveau reconnues.
- **Le Mans Ultimate, menu des stands** : « box » règle maintenant le menu avec le carburant et l'énergie prévus, « deux tours de carburant » est compris, et parler de la répartition des freins ne déclenche plus une « réparation » au stand. Côté collecteur : connexion à 127.0.0.1 (« localhost » ne répond plus depuis LMU 1.3.3), pneus neufs de la gomme montée sur les quatre roues, mode carburant relatif, et un journal détaillé (`menu-stands-lmu.txt`) ; le bouton « Tester l'API » montre tout le menu lu.
- **Deux écrans** : bouton « Nouvelle fenêtre » (⌘N) en bas de la barre de droite. La fenêtre s'ouvre sur l'autre écran avec sa propre page et ses propres préférences d'affichage ; elle est muette (la voix ne sort qu'une fois) ; les fenêtres sont rouvertes à leur place au lancement suivant.
- **Trois ordinateurs, serveur de calcul** : un troisième ordinateur (avec Ollama et de plus gros modèles) peut devenir « Serveur de calcul » au premier lancement, dans les Réglages ou à l'installation sous Windows. SecondMonitor le trouve tout seul sur le réseau (Ingénieur IA › Machines IA), s'y relie avec un code d'appairage, et ses modèles s'utilisent comme les autres (réflexion, spécialistes, questions difficiles).
- **Batterie, pendant le tour** : « batterie » donne le pourcentage, ce que le dernier tour t'a fait gagner ou perdre, où tu la vides et où tu la recharges le plus (ligne droite, virage), et en direct si tu dépenses plus ou moins qu'au même endroit du tour précédent, avec la fin de tour estimée.
- **Essence et énergie virtuelle, pendant le tour** : la consommation du tour en cours est comparée au tour précédent au même endroit, avec une estimation du tour entier ; dès le premier tour, la consommation au tour est estimée au lieu d'attendre la ligne.
- **Fin de session** : en qualification et en essais aussi, l'ingénieur annonce la position, le meilleur tour et l'écart à la pole ou au meilleur temps (« Pole position, bravo ! ») ; le message d'arrivée est dit même si les interventions spontanées sont coupées.
- **Temps perdu au stand, circuit par circuit** : avant tout arrêt, l'ingénieur connaît le temps de la voie des stands de 108 circuits (valeurs publiées pour 67, dont tous ceux de Le Mans Ultimate ; estimées d'après la longueur de la voie pour 41) et y ajoute le temps d'arrêt : au Mans, 32 s de voie et, en Le Mans Ultimate, l'arrêt calculé sur l'énergie prévue (« 10 % d'énergie et quatre pneus : 18 s »). La mesure le remplace dès qu'un arrêt est vu.

---

## Version 0.7.2

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
- **Ingénieur IA, mode thinking** : pour les modèles qui l'ont (Qwen 3, DeepSeek R1, gpt-oss…), l'IA peut raisonner avant de répondre, plus juste mais plus lente. À la radio : « active le mode thinking », « réfléchis plus », « coupe le thinking », « réponds plus vite » ; l'ingénieur dit si le modèle choisi ne l'a pas. Interrupteur dans l'écran Ingénieur IA.
- **Ingénieur IA, tutoiement** : l'ingénieur te tutoie à la radio, comme une vraie équipe.
- **Limites de piste désactivables à la voix** : « coupe les limites de piste », « remets les limites de piste » (et « coupe les alertes » pour toutes les alertes de pilotage), aussi dans l'écran Ingénieur IA.
- **Voix** : une vraie pause entre les valeurs d'une liste (les virgules n'étaient pas marquées à l'oral).
- **Pédales et volant** : nouveau bloc de l'accueil avec l'accélérateur, le frein et le volant en direct et sur tout le tour, comparés à votre meilleur tour ; l'ingénieur analyse votre pilotage (« Peux-tu analyser mon pilotage ? »).
- **Réglages et limites** : nouveau bloc de l'accueil (répartition, TC, ABS, cartographie, énergie, points de limites de piste, pénalités).
- **Sessions** : boutons « Terminer la session » et « Nouvelle session », et menu déroulant pour revoir les échanges d'une session passée (écran Ingénieur IA).
- **Dégâts** : plus de fausses « roues arrachées » en entrant au garage ou en sortant des stands.
- **Ingénieur IA, lexique de piste** : box box, pit, delta, fuel, stint, undercut, overcut, soft / medium / hard, DRS, ERS, brake bias, VE… sont compris tels quels ; les erreurs courantes de la dictée sont corrigées (« books » → box, « passion d'énergie » → gestion d'énergie, « balance de faim » → balance de frein, « ces plumes » → ces pneus). Ajoutez vos propres corrections dans la nouvelle carte « Lexique de piste » de l'écran Ingénieur IA (ex. « vexe » → BB).
- **Ingénieur IA, « Qu'est-ce qui s'est passé ? »** : votre réponse après une usure anormale est reliée à la question (sortie au vibreur, contact, blocage, patinage, attaque trop optimiste, réglage changé…) ; « ok » accepte le réglage proposé ; si vous posez aussi une question (« je peux rester sur ces pneus ? »), elle a sa réponse. Une demande d'arrêt (« box box ») n'est plus prise pour une explication.
- **Ingénieur IA, il revient vers vous** : après votre explication, l'ingénieur refait le point deux tours plus tard (« Point usure arrière droit : 1,2 % par tour, c'est revenu à la normale »).
- **Ingénieur IA, contexte** : le modèle connaît les derniers échanges radio (alertes comprises) et la question qu'il vient de poser ; « Quelle heure il est ? » donne l'heure de l'ordinateur ; une réponse différée commence par « Je reviens vers toi » quand la question n'a pas de sujet de course (plus de « Pour la voiture : … il est 14h »).
- **Ingénieur IA, écoute** : la voix part toujours en WAV 16 kHz mono (format accepté par tous les modèles « écoute »), sans les silences ; un message long est découpé sur un silence (28 s par défaut, réglable pour les modèles limités à 30 ou 60 s) et renvoyé en morceaux de 10 s si le modèle le refuse. Les autres formats audio sont convertis (ffmpeg, ou afconvert sur Mac).
- **Musique, playlist préférée** : l'étoile d'un résultat de recherche en fait votre préférée ; « Lecture » la lance quand rien ne joue, bouton « Ma playlist », « mets ma playlist » à la voix, et (si coché) elle démarre toute seule au début de chaque session.
- **Musique sur le PC de jeu** : nouveau réglage « Musique sur : le PC de jeu (casque) ». La recherche et les commandes restent sur l'ordinateur de SecondMonitor, la musique joue dans l'application Deezer du PC de jeu (case « Musique Deezer sur ce PC » du collecteur, Windows) et se met en pause quand l'ingénieur parle dans le casque.
- **Musique, lancement vérifié** : si Deezer s'ouvre sans jouer (lecture automatique bloquée par le navigateur), SecondMonitor envoie « lecture » puis explique quoi faire dans la carte Musique.
- **Réglages, voix de l'ingénieur** : interrupteur « Cet écran peut faire entendre la voix » et une ligne qui dit ce que fait cet écran et pourquoi (silencieux quand le casque du PC de jeu a la voix, ou voix ici parce qu'aucun PC de jeu ne la joue).
- **Mises à jour, même système que Multibook et ClickDeck** : SecondMonitor et le collecteur lisent le site OneDrop3D (dès qu'il est en ligne) et GitHub, gardent la version la plus haute, et acceptent les fichiers du dépôt commun des produits OneDrop3D. Téléchargement sur quatre connexions en parallèle.
- **Mises à jour, PC de jeu** : un système dont le fichier n'a pas encore été reconstruit pour la dernière version ne se voit plus proposer une version qu'il ne peut pas installer (le collecteur Windows pouvait réinstaller la même version en boucle).
- **Trajectoires** : la carte prend toute la place (et un bouton « Grand écran ») ; zoom à la molette ou au pincement du trackpad, glisser pour se déplacer, double-clic pour revenir ; puces V1, V2… et étiquettes cliquables pour zoomer directement sur un virage ; vitesse et distance de chaque pilote au survol.
- **Vitesses, « Mes tours comparés »** : jusqu'à quatre de vos tours superposés (vitesse, écart cumulé, rapport engagé, accélérateur et frein, régime), passages de rapports alignés (où, à quelle vitesse, à quel régime, écart avec la référence) et réglages en roulant de chaque tour (répartition, TC, carto…), différences surlignées.
- **Micro-secteurs** : votre configuration reste quand vous changez d'onglet ou relancez SecondMonitor, et vous pouvez en enregistrer sous un nom ; nouvelle vue « Tous les tours » (vous, des pilotes choisis, votre catégorie ou tout le plateau), filtrée par gomme (tendres, mediums, durs, intermédiaires, pluie), avec ou sans tours de stand et tours invalides.
- **Comportement des pneus** : par gomme et par catégorie, meilleur, moyenne, dégradation au fil du relais et courbe selon l'âge des pneus (ex. toutes les pluies sur toutes les voitures).
- **Analyse** : le tour qui suit la sortie des stands n'est plus écarté au Mans (la sortie est après la ligne), et un tête-à-queue ne fait plus perdre la trace du tour.

---

## Version 0.7.1

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
- **Ingénieur IA, mode thinking** : pour les modèles qui l'ont (Qwen 3, DeepSeek R1, gpt-oss…), l'IA peut raisonner avant de répondre, plus juste mais plus lente. À la radio : « active le mode thinking », « réfléchis plus », « coupe le thinking », « réponds plus vite » ; l'ingénieur dit si le modèle choisi ne l'a pas. Interrupteur dans l'écran Ingénieur IA.
- **Ingénieur IA, tutoiement** : l'ingénieur te tutoie à la radio, comme une vraie équipe.
- **Limites de piste désactivables à la voix** : « coupe les limites de piste », « remets les limites de piste » (et « coupe les alertes » pour toutes les alertes de pilotage), aussi dans l'écran Ingénieur IA.
- **Voix** : une vraie pause entre les valeurs d'une liste (les virgules n'étaient pas marquées à l'oral).
- **Pédales et volant** : nouveau bloc de l'accueil avec l'accélérateur, le frein et le volant en direct et sur tout le tour, comparés à votre meilleur tour ; l'ingénieur analyse votre pilotage (« Peux-tu analyser mon pilotage ? »).
- **Réglages et limites** : nouveau bloc de l'accueil (répartition, TC, ABS, cartographie, énergie, points de limites de piste, pénalités).
- **Sessions** : boutons « Terminer la session » et « Nouvelle session », et menu déroulant pour revoir les échanges d'une session passée (écran Ingénieur IA).
- **Dégâts** : plus de fausses « roues arrachées » en entrant au garage ou en sortant des stands.

---

## Version 0.7.0

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
- **Ingénieur IA, mode thinking** : pour les modèles qui l'ont (Qwen 3, DeepSeek R1, gpt-oss…), l'IA peut raisonner avant de répondre, plus juste mais plus lente. À la radio : « active le mode thinking », « réfléchis plus », « coupe le thinking », « réponds plus vite » ; l'ingénieur dit si le modèle choisi ne l'a pas. Interrupteur dans l'écran Ingénieur IA.
- **Ingénieur IA, tutoiement** : l'ingénieur te tutoie à la radio, comme une vraie équipe.
- **Limites de piste désactivables à la voix** : « coupe les limites de piste », « remets les limites de piste » (et « coupe les alertes » pour toutes les alertes de pilotage), aussi dans l'écran Ingénieur IA.
- **Voix** : une vraie pause entre les valeurs d'une liste (les virgules n'étaient pas marquées à l'oral).
- **Pédales et volant** : nouveau bloc de l'accueil avec l'accélérateur, le frein et le volant en direct et sur tout le tour, comparés à votre meilleur tour ; l'ingénieur analyse votre pilotage (« Peux-tu analyser mon pilotage ? »).
- **Réglages et limites** : nouveau bloc de l'accueil (répartition, TC, ABS, cartographie, énergie, points de limites de piste, pénalités).
- **Sessions** : boutons « Terminer la session » et « Nouvelle session », et menu déroulant pour revoir les échanges d'une session passée (écran Ingénieur IA).
- **Dégâts** : plus de fausses « roues arrachées » en entrant au garage ou en sortant des stands.

---

## Version 0.6.1

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
- **Voix** : une vraie pause entre les valeurs d'une liste (les virgules n'étaient pas marquées à l'oral).
- **Pédales et volant** : nouveau bloc de l'accueil avec l'accélérateur, le frein et le volant en direct et sur tout le tour, comparés à votre meilleur tour ; l'ingénieur analyse votre pilotage (« Peux-tu analyser mon pilotage ? »).
- **Réglages et limites** : nouveau bloc de l'accueil (répartition, TC, ABS, cartographie, énergie, points de limites de piste, pénalités).
- **Sessions** : boutons « Terminer la session » et « Nouvelle session », et menu déroulant pour revoir les échanges d'une session passée (écran Ingénieur IA).
- **Dégâts** : plus de fausses « roues arrachées » en entrant au garage ou en sortant des stands.

---

## Version 0.6.0

- **Mises à jour intégrées** (comme Multibook et ClickDeck) : SecondMonitor vérifie au démarrage puis toutes les 6 h, télécharge la nouvelle version (taille et SHA-256 vérifiés) et « Redémarrer et installer » remplace les deux applications ensemble, sans repasser par Réglages › Confidentialité et sécurité sur Mac. Retour automatique à l'ancienne version si la nouvelle ne démarre pas. Le collecteur du PC de jeu a aussi son bouton « Installer » (jamais pendant que le jeu envoie des données). La première version avec ce système (0.6.0) s'installe encore à la main.
- **Linux** : installer.sh vérifie les dépendances et donne la commande exacte (apt, dnf, pacman, zypper, SteamOS), options `--demarrage-auto`, `--pare-feu`, `--verifier`, `--silencieux` ; désinstallation complète avec `--tout` ; repli navigateur sans session graphique ; check-list de test dans packaging/linux/LISEZ-MOI-LINUX.md.
- **Accueil en tableau de bord personnalisable** : bouton « Disposition » dans la barre du haut pour déplacer les blocs (glisser, au doigt sur iPad et téléphone), changer leur largeur et leur hauteur, les masquer, en ajouter ou tout réinitialiser. Disposition enregistrée sur chaque appareil (grand écran, tablette, téléphone sur une colonne). Blocs : Classement (mini-secteurs violet / vert / jaune avec temps de secteur, intervalle, places gagnées ▲▼, arrêts, pneu et âge), Carte du circuit, Radio (derniers échanges pilote ↔ ingénieur, réécoute de la voix), Événements de course (drapeaux, arrêts des voisins, meilleurs tours, tours invalidés, abandons, alertes pneus et dégâts, notifications passées, filtre par type), Ma voiture, Prochain arrêt, Meilleurs tours, Duels, Dégâts, Météo, Delta.
- **Barre du haut** : nouveau bloc temps restant / tours restants avec barre d'avancement ; en course au temps, le hub estime vos tours restants (et ceux du leader) à partir du rythme réel et de la règle de fin (le tour en cours se termine quand le temps est écoulé). État de la session, vent et piste mouillée affichés.
- **Notifications** : chacune a sa croix, disparaît seule, ne revient pas en boucle pour le même événement et reste consultable dans « Événements de course » (correction : la notification « Données reçues » pouvait rester bloquée). Les alertes pneus / dégâts et les drapeaux se masquent aussi d'une croix.
- **Musique Deezer** (carte Musique dans Réglages) : la musique joue dans l'application Deezer de l'ordinateur (pleine qualité, votre compte) et se commande à la voix sans passer par l'IA (« mets mon Flow », « mets la playlist rock », « mets Daft Punk », « pause musique », « chanson suivante », « remets la précédente », « monte / baisse la musique », « c'est quoi ce morceau ? ») ; la radio passe avant (pause puis reprise, ou volume de Deezer baissé sur Windows / Linux) ; option « pas de musique en course » ou « seulement en essais ».
- **Collecteur plus léger pour le PC de jeu** (même protocole, rien de perdu pour SecondMonitor) : quand le jeu se tait (menus, pause, garage), le signe de vie d'une seconde ne renvoie plus tout le plateau identique (≈ 18 Ko/s → 0,2 Ko/s en F1 24) ; recherche du jeu en un seul passage sur les processus (3× moins d'allocations, 2× moins de CPU sans jeu) ; envois sans copies inutiles (−38 % d'allocations en course) ; fenêtre mise à jour seulement quand quelque chose change, et presque plus quand elle est réduite. Option **Diagnostic** (Réglages, ou `--diagnostic`) : CPU, mémoire, débit réseau réel et lectures du jeu par seconde. `--rejouer fichier.smrec` renvoie un enregistrement de SecondMonitor pour mesurer sans le jeu.
- **Ingénieur IA** : réponses chiffrées courtes et instantanées (meilleur / pire / dernier tour, moyenne, régularité, « Vitesse max 204 km/h au tour 7, en moyenne 202 km/h ; le plus rapide est le numéro 52 à 208 km/h » avec un conseil de sortie de virage ou d'aileron en cas de déficit) ; base « symptôme → réglages » (sous-virage / survirage selon la phase, blocages, roue intérieure, patinage, freinage instable, rebonds, pneus, pluie) adaptée au jeu et à la voiture, sans attendre le modèle ; fil de conversation (« oui », « non » → « Copy, compris », « autre chose ? », « c'est en sortie », « c'est mieux »), propositions refusées jamais reproposées ; chaque échange classé (Stratégie, Réglages de course, Réglages de la voiture, Informations course) ; **Carnet** de session enregistré automatiquement (dossier « carnets ») avec récapitulatif de fin de session (IA ou calculé) ; radio d'arrivée avec le résultat ; conseils batterie pour les hybrides (F1 : ERS, LMU : hypercars) ; quand une donnée manque, l'ingénieur dit laquelle et pourquoi.
- **Tracés réels de circuits** : la bibliothèque passe de 40 à 130 circuits et variantes (ACC au complet, Le Mans Ultimate, Assetto Corsa, F1, rFactor 2, Automobilista 2 avec les circuits brésiliens, RaceRoom avec les circuits allemands et scandinaves, iRacing, Project CARS 2), d'après OpenStreetMap (© contributeurs OpenStreetMap, ODbL) et bacinger/f1-circuits (MIT). La ligne de départ et le sens de course sont recalés automatiquement sur les positions des voitures dans le jeu ; tant qu'un tracé n'est pas sûr, il est signalé « départ estimé » ou n'est pas affiché.

## Version 0.5.0

- **Dégâts mécaniques** pièce par pièce, quand le jeu les donne :
  - F1 24 : ailerons avant gauche et droit, aileron arrière, fond plat, diffuseur, pontons, boîte, moteur, usure de chaque élément moteur (MGU-K, MGU-H, batterie…), pannes DRS et ERS, pneus abîmés ;
  - ACC : carrosserie par zone et suspension par roue ;
  - Le Mans Ultimate : carrosserie, pièce arrachée, surchauffe, crevaisons ;
  - les autres jeux : moteur, boîte, suspension, carrosserie, freins.

  Un nouveau dégât est signalé tout de suite. Son coût par tour est mesuré deux tours plus tard, et l'ingénieur dit si un arrêt pour réparer est rentable.
- **Stratégie d'arrêt calculée** :
  - perte au stand et gain des pneus neufs mesurés sur tout le plateau ;
  - position à la sortie si vous rentrez maintenant ;
  - **undercut** et **overcut** sur la voiture devant ;
  - **menace d'undercut** de la voiture derrière.

  Le tout est affiché dans l'écran Stratégies et le panneau du Chronométrage, et l'ingénieur prévient quand un undercut est jouable.
- **Ingénieur IA** :
  - il connaît le vocabulaire de course (undercut, overcut, relais, tour de sortie, graining, carrossage…) et recopie les calculs ;
  - il s'adapte à la session : la voiture en essais (compte rendu de tour avec pressions et carrossage), le tour lancé en qualification, le déroulé en course ;
  - les questions rapides changent selon la session.
- **Tracés réels** de 40 circuits (Silverstone, Spa, Monza, Le Castellet, Suzuka, COTA, Interlagos, Imola, Portimão, Losail…). Ils s'affichent dès le premier tour au lieu du cercle, puis sont remplacés par le tracé appris en roulant. Un tracé n'est utilisé que si le nom et la longueur du circuit correspondent.
- **Bouton radio** où l'on veut :
  - une touche du clavier ou un bouton du volant du **PC de jeu**, réglé dans la fenêtre du collecteur ; il marche même quand le jeu est au premier plan (Windows) ;
  - le volant branché sur l'ordinateur qui affiche ;
  - une touche ClickDeck.

  L'écran « poste radio » se choisit dans les Réglages.
- **Essais et qualification** : le panneau de droite du Chronométrage montre la voiture (meilleur tour, écart, pneus sur le dernier tour, pressions) à la place des duels.
- Nouvelle adresse `/api/deck` : toutes les données de course sous une forme compacte, pour ClickDeck et les petits écrans. `POST /api/radio/press` pour un bouton radio externe.
- En mode serveur sans fenêtre, plus de fenêtre surprise si le port est déjà pris.

## Version 0.4.1

- **Écran noir corrigé** : une valeur de calcul impossible (division par zéro sur certaines données de jeu) faisait planter l'interface. Ces valeurs s'affichent maintenant « — ». Si un écran rencontre malgré tout un problème, il l'affiche et les autres continuent.
- **Voir que ça marche** : nouvel écran **Connexion** avec trois étapes (collecteur, jeu, données) et un bandeau vert « Tout fonctionne ». Il propose aussi une saisie simple de l'adresse du PC. Une notification apparaît quand les données arrivent, et l'état reste affiché dans la barre du bas.
- **Guide de mise en route** au premier lancement : connexion, ingénieur IA (Ollama, modèles, test), bouton radio et volant.
- Le jeu ouvert mais muet n'est plus pris pour une course (avant, l'écran restait vide sans explication). Le collecteur et SecondMonitor disent quoi régler, par exemple la télémétrie UDP de F1 24.
- **Assetto Corsa** (aussi avec Content Manager) : le collecteur propose d'installer le plugin nécessaire (avant, la demande était perdue et le jeu n'était jamais détecté). Un bouton permet de relancer en administrateur si besoin.
- **Fenêtre du collecteur** plus claire : grand bandeau d'état, trois étapes cochées, conseils, réglages repliés.
- **Ingénieur IA** :
  - rythme calculé (le vôtre, l'objectif pour rattraper ou gagner, la projection) ;
  - tendance météo à 30 minutes ;
  - réponse à toutes les questions posées d'un coup ;
  - une phrase dictée sans « ? » est quand même comprise comme une question.
- **Alertes pneus** : moyenne de température par tour, alerte quand un pneu est trop chaud sur tout un tour ou depuis plus de 30 s, pneus froids au tour de formation. Toutes les sessions sont couvertes, pas seulement la course. Le pneu concerné est encadré en rouge à l'écran.
- Diagnostic : enregistrement de ce que le collecteur envoie, rejouable (`--rejouer`), et `journal.txt`.
- Windows : la vue web utilise un dossier de travail dans le profil de l'utilisateur.

## Version 0.4.0

- **Deux vraies applications, installées ensemble** sur Windows, Mac et Linux : **SecondMonitor** (affiche les données, dans sa propre fenêtre, sans navigateur) et **SecondMonitor Collecteur** (PC de jeu, avec une petite fenêtre d'état : connexion, jeu détecté, débit, qualité, démarrage automatique, démonstration, journal).
- **Écran « Chronométrage » repris de SecondMonitor d'origine** :
  - bandeau de session avec la météo, les deltas en direct (meilleur tour, tour précédent), les meilleurs secteurs, le meilleur tour et le temps restant ;
  - grand tableau avec les secteurs violets et verts et le numéro du tour du meilleur temps ;
  - les quatre roues, le carburant, le moteur, la carte, les duels et la stratégie ;
  - barre d'écrans à droite et barre d'état en bas.
- Installateurs : `.exe` pour Windows (Inno Setup, choix des applications, raccourcis, désinstallation), `.dmg` pour Mac (les deux applications), `.tar.gz` + `installer.sh` pour Linux (menu des applications).
- Corrigé : sur Mac, l'application quittait de manière imprévue quand une autre copie tournait déjà (port 8686 occupé). Désormais elle ouvre simplement la copie en cours, ou explique quel programme occupe le port.
- Le collecteur range ses réglages dans le dossier de l'utilisateur (l'application installée peut être en lecture seule).
- Arrondis homogènes sur tous les blocs. Thème sombre par défaut, comme l'original.

## Version 0.3.1

- Première version publiée : hub pour Mac (Apple Silicon et Intel), Windows et Linux ; collecteur pour Windows et Linux/SteamOS.
- Construction automatique de tous les systèmes (OneDrop3D et GitHub Actions).

---

## Version 0.3.0

- **Collecteur** : petit programme pour le PC de jeu (Windows, Linux/SteamOS) qui remplace SecondMonitor complet, avec les mêmes jeux et une charge minimale.
- **Ingénieur IA** : voix sans chiffres mal lus, coupure de la radio d'un appui, prise de parole spontanée, mémoire de course, changement de modèle à la voix, plusieurs machines IA.
- **Analyses** : micro-secteurs, Vmax/Vmin et trajectoires, télémétrie pneus/freins/moteur, météo et tendance, prévisions.
- **Interface** : centrée sur votre catégorie, adaptée à toutes les tailles d'écran, logos des marques, graphiques agrandissables.

---
