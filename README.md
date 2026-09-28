# Terrain To Mesh

Un plugin Unity Editor qui convertit un `Terrain` Unity (qui n'a pas de mesh natif) en un vrai mesh 3D, exportable en **OBJ** ou **FBX**, avec génération automatique d'une **normal map** à partir de la heightmap.

## Pourquoi ce plugin ?

Nativement, un `Terrain` Unity n'est pas un objet avec un `MeshFilter` : c'est un système à part, basé sur une heightmap (`TerrainData`). Impossible donc de l'exporter directement vers un logiciel 3D (Blender, Maya...) ou un autre moteur. Ce plugin reconstruit un mesh classique à partir de la heightmap, pour pouvoir :

- Exporter le terrain en `.obj` ou `.fbx`
- Générer une normal map correspondant au relief du terrain

## Installation

### Via Git URL (recommandé)

1. Dans Unity, ouvre **Window > Package Manager**.
2. Clique sur le bouton **"+"** en haut à gauche.
3. Choisis **"Add package from git URL..."**.
4. Colle l'URL suivante :

```
https://github.com/klaivertt/terraintomesh.git
```

### Manuellement

1. Télécharge ou clone ce dépôt.
2. Copie le dossier dans le `Packages/` de ton projet Unity.
3. Unity détecte automatiquement le package au redémarrage ou après un rafraîchissement.

## Prérequis

- Unity **6000.4** ou supérieur.
- Pour l'export **FBX** uniquement : le package officiel **FBX Exporter** (`com.unity.formats.fbx`), installable depuis le Package Manager (Unity Registry). L'export **OBJ** ne nécessite aucune dépendance.

## Utilisation

1. Ouvre la fenêtre de l'outil via **Tools > Terrain To Mesh**.
2. Glisse ton `Terrain` dans le champ **Terrain**.
3. Choisis le format d'export (**OBJ** ou **FBX**) dans le menu déroulant.
4. Clique sur **Convert**.

Le plugin génère :
- Un fichier de mesh (`.obj` ou `.fbx`) correspondant à la géométrie exacte du terrain.
- Une normal map (`.png`) calculée à partir des pentes de la heightmap.

Les fichiers sont créés dans le dossier `Assets/` du projet.

> **Note :** après génération, pense à sélectionner le fichier PNG de normal map dans le Project et à changer son **Texture Type** en **Normal Map** dans l'Inspector, pour qu'il soit correctement interprété par les shaders.

## Limitations connues

- Un terrain avec une heightmap haute résolution (ex. 4097×4097) génère un mesh très lourd (plusieurs millions de vertices). Pas encore d'option de réduction de résolution ou de découpage en chunks.
- Le mesh généré n'inclut pas les textures de splatmap du terrain, ni les objets placés dessus (arbres, détails).
- La normal map est calculée par différences finies simples (pas de filtre de Sobel), suffisant pour un usage courant.

## Roadmap

- [ ] Option de réduction de résolution du mesh
- [ ] Export des textures/splatmap du terrain
- [ ] Découpage en chunks pour les très grands terrains
- [ ] Boîte de dialogue pour choisir le chemin de sortie

## Licence

MIT — libre d'utilisation, de modification et de redistribution.

## Auteur

klaivert
