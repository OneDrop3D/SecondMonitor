<!--
  NOUVEAUTÉS À PUBLIER — fichier lu par le Publieur OneDrop3D.
  POUR L'IA DE CODE : quand tu termines des modifications sur cette application, ajoute ici,
  sous ce commentaire, une ligne par point visible par l'utilisateur (Markdown, en français,
  ex. « - **Réglages** : le thème sombre s'applique aussi à la fenêtre d'aide »).
  Mets aussi à jour le README / la documentation si une fonction change.
  Ne mets pas de numéro de version : le Publieur le gère. Il vide ce fichier après la publication.
-->
- **Windows, fenêtre noire au lancement** : l'interface s'ouvre dans le navigateur au bout de 6 s au lieu de 15, et SecondMonitor retient de démarrer la fenêtre sans accélération graphique au lancement suivant (cause habituelle d'une fenêtre WebView2 noire).
- **Mises à jour automatiques** : avec « Vérifier automatiquement » coché, la nouvelle version se télécharge et s'installe toute seule (SecondMonitor redémarre), jamais pendant une session de jeu : elle attend que le jeu n'envoie plus rien depuis 2 minutes. Même chose pour le collecteur du PC de jeu. Une version mise de côté avec « Plus tard » ne s'installe pas toute seule.
- **PC de jeu relié par câble** (Ethernet, adaptateur USB-C vers Ethernet, ou câble USB4 / Thunderbolt quand le PC le permet) : SecondMonitor cherche maintenant le collecteur sur toutes les connexions du Mac, pas seulement la principale ; plus besoin de saisir l'adresse du câble. Nécessite le collecteur à jour sur le PC de jeu.
