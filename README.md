# Trabajo Práctico: Simulador con Articulation Body
**Mecanismo:** Grúa Torre con Pinza Clasificadora

## 1. Explicación del Simulador
- **Máquina simulada y operación:** El prototipo simula una Grúa Torre equipada con una pinza de agarre. La maniobra principal consiste en trasladar y clasificar cargas (representadas por cajas/cubos rígidos), agarrándolas desde un punto, levantándolas mediante el cable y el carro, y depositándolas en otro lugar.
- **Controles:** El mecanismo se controla mediante un script controlador que recibe *inputs* del usuario para modificar dinámicamente la propiedad `targetPosition` de los *Articulation Drives* controlados (mástil, carro, cable y pinzas). Una interfaz de usuario (UI) actualiza en pantalla la posición real vs el objetivo de cada articulación en tiempo real.
- **Elección de los valores de los drives:** 
  - Para los ejes de traslación pesada (**Carro y Cable**), se eligieron valores muy altos de `stiffness` (rigidez) y `forceLimit` simulando la potencia de los motores industriales, evitando que la carga ceda por la gravedad.
  - El pivote pasivo del **Gancho** (Spherical) tiene `stiffness` en 0, permitiendo que la gravedad y la inercia actúen de forma natural para generar balanceo.
  - Para las **Pinzas** (Revolute), el `stiffness` y el `forceLimit` se redujeron deliberadamente, mientras que se aumentó el `damping` (amortiguación). Esto se hizo para que actúen como un resorte suave que "abraza" el cubo sin aplastarlo, evitando así que el motor de físicas dispare el objeto por errores de interpenetración (depenetration).

## 2. Diagrama de Árbol del Mecanismo

El siguiente esquema muestra la jerarquía de los Articulation Bodies, incluyendo los tipos de joint, ejes de movimiento y límites de las uniones:

```text
[Base] (Raíz inamovible)
  │
  └── [Mástil] (Joint: Fixed)
        │
        └── [Brazo] (Joint: Fixed)
              │
              └── [Carro / Trolley] 
                    - Joint: Prismatic (Activo)
                    - Eje: Z (A lo largo del brazo)
                    - Límites: [0, 15] (Recorrido sobre el brazo)
                    │
                    └── [Cable]
                          - Joint: Prismatic (Activo)
                          - Eje: Y (Vertical)
                          - Límites: [-10, 0] (Altura del piso al carro)
                          │
                          └── [Base del Gancho]
                                - Joint: Spherical (Pasivo / Movido por gravedad)
                                - Ejes: Libres (Swing Y/Z)
                                - Límites: Swing Limit 30° (Cono de balanceo)
                                │
                                ├── [Pinza Izquierda]
                                │     - Joint: Revolute (Activo)
                                │     - Eje: X
                                │     - Límites: [0, 45] grados (Apertura)
                                │
                                └── [Pinza Derecha]
                                      - Joint: Revolute (Activo)
                                      - Eje: X
                                      - Límites: [-45, 0] grados (Apertura)