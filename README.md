# **Simugias VR – Endoscopic Medical Simulator**

**Unity 2022 LTS • XR Interaction Toolkit • OpenXR • Meta Quest 2/3**
Sprint de Cierre: **28 Nov → 5 Dic**

---

## 🩺 **Descripción del Proyecto**

**Simugias VR** es un simulador médico enfocado en recrear un procedimiento endoscópico básico en un entorno de realidad virtual.
El objetivo es permitir que el usuario complete una secuencia quirúrgica funcional utilizando herramientas VR:

* Bisturí (Scalpel)
* Retractor
* Taladro (Drill)
* Marcador (Marker)
* Endoscopio (Endoscope)

Esta versión del repositorio corresponde a la **fase de cierre del simulador**, orientada exclusivamente a integración técnica y funcionalidad.

---

## 🎯 **Objetivo del Sprint Actual**

Implementar **toda la secuencia del procedimiento** en VR:

1. Incisión
2. Retracción
3. Perforación
4. Navegación endoscópica

Las herramientas ya existen como prefabs, pero deben **ser integradas completamente** al sistema de interacción VR.

---

## 🧰 **Estado Actual del Proyecto**

✔ El proyecto abre sin errores
✔ XR Origin funcionando
✔ Modelo del paciente riggeado (con huesos para retracción)
✔ Todas las herramientas están dentro del proyecto
✔ URP configurado
✘ Herramientas aún no integradas
✘ Endoscopio vacío
✘ HUD simple pendiente
✘ Flujo completo sin implementar

---

## 🛠 **Tecnologías Utilizadas**

* **Unity 2022 LTS**
* **XR Interaction Toolkit (Action-Based)**
* **OpenXR**
* **URP (Universal Render Pipeline)**
* **Meta Quest 2 / Meta Quest 3**

---

## 📁 **Estructura del Proyecto**

```
Assets/
│── Scenes/
│     └── VR_MainScene.unity
│
│── Scripts/
│     ├── Tools/
│     ├── Systems/
│     └── VR/
│
│── Models/
│     ├── Patient/
│     └── Tools/
│
│── Prefabs/
│     ├── Tools/
│     └── Environment/
│
│── Materials/
│── Shaders/
│── Textures/
```

---

## 🩹 **Estado de las Herramientas (antes del sprint)**

| Herramienta | Estado                              |
| ----------- | ----------------------------------- |
| Scalpel     | Sin integración                     |
| Retractor   | Sin integración                     |
| Drill       | Sin integración                     |
| Marker      | No pinta                            |
| Endoscope   | Vacío (sin movimiento ni detección) |

---



### **Fase 1 – Integración técnica**

* Estabilizar Input System
* Integrar Scalpel (path)
* Integrar Retractor (bones)
* Integrar Drill (snap + perforación)
* Integrar Marker (painting UV)
* Integrar Endoscopio (spline + highlight)

### **Fase 2 – Procedimiento**

* Construir el flujo completo
* HUD básico para guiar al usuario

### **Fase 3 – Finalización**

* Fixes según testing
* Ajustes de iluminación URP
* Build final para Quest

---

## 🔧 **Cómo Ejecutar el Proyecto**

### **Requisitos**

* Unity **2022.x LTS**
* XR Interaction Toolkit instalado
* OpenXR configurado
* URP configurado
* Android Build Support habilitado
* Meta XR Plugin instalado

### **Pasos**

1. Clonar el repositorio
2. Abrir `VR_MainScene.unity`
3. Seleccionar plataforma Android
4. Configurar OpenXR como sistema de XR
5. Build & Run hacia Meta Quest 2/3

---

## 🧪 **Testing**

Todo testeo se realiza en Meta Quest 2/3.

Los videos de pruebas y feedback se almacenan en:

👉 **Drive/Test Videos**
(Enlace privado entregado al equipo)

---

## 📝 **Contribución**

* El trabajo se organiza en Trello
* Cada tarea tiene:
  ✔ Descripción
  ✔ Checklist
  ✔ Fecha límite
  ✔ Responsable
* Nada pasa a “Completado” sin un video de testeo en Quest

---

## 📌 **Deadline**

**Entrega final funcional del simulador: 5 de diciembre.**

---

## 👤 **Contacto**
estebanfmunoz22@gmail.com
**Project Lead:** Esteban
Testing diario, feedback y aprobación final.

---
