# Tracés issus d'OpenStreetMap

Les tracés de `circuits.json` dont la source est « OpenStreetMap (ODbL) » sont dérivés des données
d'OpenStreetMap : **© contributeurs OpenStreetMap**, https://www.openstreetmap.org/copyright

Ces données sont disponibles sous la licence Open Database License (ODbL) 1.0 :
https://opendatacommons.org/licenses/odbl/1-0/

Base de données dérivée : les tracés ont été reconstitués (voies `highway=raceway`), rééchantillonnés
(400 points, mètres entiers) et orientés par `Hub/tools/circuits.py`. Ce fichier `circuits.json`, en tant que
base de données dérivée, est lui-même mis à disposition sous ODbL 1.0 pour sa partie issue d'OpenStreetMap ;
le script `Hub/tools/circuits.py` permet de le reproduire à partir des données publiques.

Les tracés dont la source est « bacinger/f1-circuits (MIT) » relèvent de LICENCE-f1-circuits.md
(départs et sens éventuellement corrigés à l'aide d'OpenStreetMap).
