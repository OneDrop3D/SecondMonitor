<!--
  NOUVEAUTÉS À PUBLIER — fichier lu par le Publieur OneDrop3D.
  POUR L'IA DE CODE : quand tu termines des modifications sur cette application, ajoute ici,
  sous ce commentaire, une ligne par point visible par l'utilisateur (Markdown, en français,
  ex. « - **Réglages** : le thème sombre s'applique aussi à la fenêtre d'aide »).
  Mets aussi à jour le README / la documentation si une fonction change.
  Ne mets pas de numéro de version : le Publieur le gère. Il vide ce fichier après la publication.
-->
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
