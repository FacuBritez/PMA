# <img src="./readme-assets/puerta18-logo.webp" align="center" height="50px"> PMA/Shaders

[![Puerta-18](https://custom-icon-badges.demolab.com/badge/Puerta--18-%23ed2180.svg?logo=Instagram&style=flat-square)](https://www.instagram.com/puerta18/) [![C#](https://custom-icon-badges.demolab.com/badge/C%23-%236920b3.svg?logo=cshrp&logoColor=white&style=flat-square)](#) [![Unity](https://img.shields.io/badge/Unity-%23000000.svg?logo=unity&logoColor=white&style=flat-square)](#) 

Este repositorio es un fork del [Repositorio PMA de Puerta 18](https://github.com/FacuBritez/PMA) enfocado en el apartado de Shaders.

## 🎯 Objetivos
- Personajes:
  - Eliminar fondo de la textura 
  - Generar movimiento de los peces
- Fondo:
  - Generar movimiento de los elementos del fondo
  - Generar contraste dinámico para generar profundidad


## 🗺️ Flujo del Shader Principal
<div align="center">

```mermaid
flowchart TD
    A[Imagen Escaneada]-->|Se lee como| H
    H[Textura RGBA]
    --> I(Shader Alpha: Elimina el fondo) --> J
    J(Shader Ondular: Genera sensación de movimiento) --> K
    K[Dibujo en movimiento: Plano con textura RGBA + shaders] -->|Se integra a| L
    L[Escena en Unity]
```
</div>

## 📂 Estructura
    /Assets
    ├── Prefabs/
    │   ├── Criatura/
    │   └── Fondo/
    └── Shaders/
        ├── PostProcesado/
        └── Superficie/  

## 💻 Stack
-  <img src="./readme-assets/Csharp_Logo.webp" align="center" height="20px"> C#
- <img src="./readme-assets/Unity_logo.webp" align="center" height="20px"> HLSL

---

<div align="center">

**Francisco y Ramón · Puerta 18 · 2026**

</div>