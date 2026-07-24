# -*- coding: utf-8 -*-
"""Generador procedural de retratos de viajeros — MAIQUETÍA: Punto de Control.

Construye bustos low-poly modulares (cabeza, pelo, ropa, accesorios) con un
PRNG sembrado por índice, y los renderiza en Workbench (colores planos +
contorno) como PNGs para Unity.

Por cada viajero produce:
  retrato_NNN.png      -> el viajero en la ventanilla
  retrato_NNN_alt.png  -> variante (pelo/lentes/vello facial distinto) para
                          discrepancias de foto en el pasaporte

Uso:
  blender --background --python generar_retratos.py -- <carpeta_salida> <cantidad>
"""
import bpy
import math
import random
import sys
import os

ARGS = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
SALIDA = ARGS[0] if len(ARGS) > 0 else os.path.join(os.path.dirname(__file__), "salida_retratos")
CANTIDAD = int(ARGS[1]) if len(ARGS) > 1 else 64
os.makedirs(SALIDA, exist_ok=True)

# ---------------------------------------------------------------- paletas ---
# Diversidad venezolana: piel de todos los tonos
PIELES = [
    (0.95, 0.80, 0.69), (0.87, 0.68, 0.54), (0.76, 0.57, 0.42),
    (0.62, 0.44, 0.31), (0.48, 0.33, 0.22), (0.35, 0.24, 0.16),
]
PELOS = [
    (0.05, 0.04, 0.03), (0.15, 0.09, 0.05), (0.30, 0.18, 0.08),
    (0.55, 0.38, 0.15), (0.75, 0.75, 0.72), (0.20, 0.20, 0.20),
]
ROPAS = [
    (0.55, 0.12, 0.12), (0.13, 0.30, 0.50), (0.15, 0.40, 0.25),
    (0.60, 0.45, 0.15), (0.35, 0.25, 0.45), (0.25, 0.25, 0.28),
    (0.72, 0.55, 0.40), (0.50, 0.30, 0.15), (0.85, 0.80, 0.70),
    (0.11, 0.11, 0.35),
]
FONDO = (0.82, 0.80, 0.75)  # gris cálido tipo foto de pasaporte


def limpiar_escena():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def color_obj(obj, rgb):
    """Workbench en modo OBJECT usa obj.color — sin materiales, render veloz."""
    obj.color = (*rgb, 1.0)


def crear(nombre, tipo_op, rgb, **kwargs):
    tipo_op(**kwargs)
    o = bpy.context.active_object
    o.name = nombre
    color_obj(o, rgb)
    return o


# ------------------------------------------------------------ construcción ---
def construir_busto(rng, alt=False):
    """Construye un busto completo. `alt=True` altera pelo/lentes/vello para
    la variante de foto-que-no-coincide."""
    piel = rng.choice(PIELES)
    pelo_color = rng.choice(PELOS)
    ropa = rng.choice(ROPAS)

    # --- cabeza: esfera achatada con variación de proporciones
    ancho = rng.uniform(0.85, 1.05)
    alto = rng.uniform(1.05, 1.25)
    cabeza = crear("Cabeza", bpy.ops.mesh.primitive_uv_sphere_add, piel,
                   segments=24, ring_count=16, radius=1.0, location=(0, 0, 1.55))
    cabeza.scale = (ancho, 0.95, alto)

    # mandíbula/mentón: leve estiramiento inferior con un lattice barato: escala Z ya lo sugiere
    # --- cuello y torso
    crear("Cuello", bpy.ops.mesh.primitive_cylinder_add, piel,
          radius=0.32, depth=0.7, location=(0, 0, 0.55))
    torso = crear("Torso", bpy.ops.mesh.primitive_cube_add, ropa,
                  size=1.0, location=(0, 0, -0.35))
    torso.scale = (1.55, 0.75, 0.85)
    bev = torso.modifiers.new("Bisel", 'BEVEL')
    bev.width, bev.segments = 0.22, 4

    # hombros redondeados
    for lado in (-1, 1):
        h = crear("Hombro", bpy.ops.mesh.primitive_uv_sphere_add, ropa,
                  segments=16, ring_count=12, radius=0.42,
                  location=(lado * 1.30, 0, 0.05))
        h.scale = (1.0, 0.75, 0.9)

    # --- orejas
    for lado in (-1, 1):
        o = crear("Oreja", bpy.ops.mesh.primitive_uv_sphere_add, piel,
                  segments=12, ring_count=8, radius=0.16,
                  location=(lado * ancho * 0.98, 0, 1.55))
        o.scale = (0.5, 0.8, 1.2)

    # --- nariz: variación notable (rasgo de identidad)
    nariz_ancho = rng.uniform(0.10, 0.20)
    nariz_largo = rng.uniform(0.18, 0.34)
    n = crear("Nariz", bpy.ops.mesh.primitive_cone_add, piel,
              vertices=6, radius1=nariz_ancho, depth=nariz_largo,
              location=(0, -0.95, 1.45), rotation=(-math.pi / 2, 0, 0))
    n.scale = (1.0, 1.0, 1.0)

    # --- ojos (esclerótica + iris oscuro), separación variable
    sep = rng.uniform(0.34, 0.46)
    ojo_r = rng.uniform(0.11, 0.15)
    for lado in (-1, 1):
        e = crear("Ojo", bpy.ops.mesh.primitive_uv_sphere_add, (0.95, 0.95, 0.92),
                  segments=12, ring_count=8, radius=ojo_r,
                  location=(lado * sep, -0.82, 1.68))
        e.scale = (1.0, 0.4, 1.0)
        crear("Iris", bpy.ops.mesh.primitive_uv_sphere_add, (0.08, 0.06, 0.04),
              segments=10, ring_count=6, radius=ojo_r * 0.45,
              location=(lado * sep, -0.90, 1.68))

    # --- cejas
    ceja_grosor = rng.uniform(0.05, 0.11)
    ceja_angulo = rng.uniform(-0.25, 0.15)
    for lado in (-1, 1):
        c = crear("Ceja", bpy.ops.mesh.primitive_cube_add, pelo_color,
                  size=1.0, location=(lado * sep, -0.88, 1.90))
        c.scale = (0.22, 0.05, ceja_grosor)
        c.rotation_euler = (0, lado * ceja_angulo, 0)

    # --- boca: caja fina, ancho/curva variable
    boca_ancho = rng.uniform(0.25, 0.42)
    b = crear("Boca", bpy.ops.mesh.primitive_cube_add, (0.55, 0.25, 0.22),
              size=1.0, location=(0, -0.92, 1.18))
    b.scale = (boca_ancho, 0.04, 0.05)

    # ------------------------------------------------ rasgos variables (alt) --
    # El PRNG ya consumió lo estructural; ahora rasgos que la foto puede
    # contradecir. La variante alt GARANTIZA diferencias visibles: otro pelo,
    # lentes invertidos y otro vello facial.
    ESTILOS = ["calvo", "corto", "afro", "largo", "mono", "gorra"]
    rasgos = random.Random(int(rng.random() * 1e9))

    estilo_pelo = rasgos.choice(ESTILOS)
    tiene_lentes = rasgos.random() < 0.30
    vello = rasgos.choice(["nada", "nada", "bigote", "barba"])
    if alt:
        estilo_pelo = rasgos.choice([e for e in ESTILOS if e != estilo_pelo])
        tiene_lentes = not tiene_lentes
        vello = rasgos.choice([v for v in ("nada", "bigote", "barba") if v != vello])

    # Regla de oro del pelo: su superficie frontal debe quedar DETRÁS del
    # plano de la cara (y > -0.85) para no taparla desde la cámara frontal.
    tope = 1.55 + alto  # coronilla real de esta cabeza

    if estilo_pelo == "corto":
        p = crear("Pelo", bpy.ops.mesh.primitive_uv_sphere_add, pelo_color,
                  segments=20, ring_count=12, radius=1.02,
                  location=(0, 0.22, 1.66))
        p.scale = (ancho * 1.04, 0.85, alto * 0.98)
        fleco = crear("Fleco", bpy.ops.mesh.primitive_cube_add, pelo_color,
                      size=1.0, location=(0, -0.55, tope - 0.18))
        fleco.scale = (ancho * 0.85, 0.45, 0.14)
    elif estilo_pelo == "afro":
        p = crear("Pelo", bpy.ops.mesh.primitive_uv_sphere_add, pelo_color,
                  segments=16, ring_count=12, radius=1.22,
                  location=(0, 0.40, tope - 0.15))
        p.scale = (1.10, 0.95, 0.85)
    elif estilo_pelo == "largo":
        p = crear("Pelo", bpy.ops.mesh.primitive_uv_sphere_add, pelo_color,
                  segments=20, ring_count=12, radius=1.10,
                  location=(0, 0.30, 1.60))
        p.scale = (ancho * 1.06, 1.0, alto * 1.06)
        melena = crear("Melena", bpy.ops.mesh.primitive_cube_add, pelo_color,
                       size=1.0, location=(0, 0.40, 0.6))
        melena.scale = (0.95, 0.45, 1.1)
        fleco = crear("Fleco", bpy.ops.mesh.primitive_cube_add, pelo_color,
                      size=1.0, location=(0, -0.52, tope - 0.15))
        fleco.scale = (ancho * 0.90, 0.50, 0.16)
    elif estilo_pelo == "mono":
        p = crear("Pelo", bpy.ops.mesh.primitive_uv_sphere_add, pelo_color,
                  segments=20, ring_count=12, radius=1.03,
                  location=(0, 0.24, 1.64))
        p.scale = (ancho, 0.82, alto * 0.94)
        crear("Mono", bpy.ops.mesh.primitive_uv_sphere_add, pelo_color,
              segments=12, ring_count=8, radius=0.35,
              location=(0, 0.55, tope + 0.15))
    elif estilo_pelo == "gorra":
        crear("Gorra", bpy.ops.mesh.primitive_cylinder_add,
              rasgos.choice(ROPAS), radius=ancho * 1.14, depth=0.55,
              location=(0, 0.05, tope - 0.05))
        v = crear("Visera", bpy.ops.mesh.primitive_cube_add, (0.12, 0.12, 0.14),
                  size=1.0, location=(0, -1.05, tope - 0.22))
        v.scale = (0.85, 0.55, 0.06)
    # "calvo": nada

    if tiene_lentes:
        for lado in (-1, 1):
            aro = crear("Lente", bpy.ops.mesh.primitive_torus_add, (0.05, 0.05, 0.06),
                        major_radius=ojo_r * 1.9, minor_radius=0.025,
                        location=(lado * sep, -0.95, 1.68),
                        rotation=(math.pi / 2, 0, 0))
        puente = crear("Puente", bpy.ops.mesh.primitive_cube_add, (0.05, 0.05, 0.06),
                       size=1.0, location=(0, -0.97, 1.70))
        puente.scale = (sep * 0.6, 0.02, 0.02)

    if vello == "bigote":
        m = crear("Bigote", bpy.ops.mesh.primitive_cube_add, pelo_color,
                  size=1.0, location=(0, -0.97, 1.31))
        m.scale = (0.30, 0.06, 0.07)
    elif vello == "barba":
        bb = crear("Barba", bpy.ops.mesh.primitive_uv_sphere_add, pelo_color,
                   segments=16, ring_count=10, radius=0.85,
                   location=(0, -0.25, 1.05))
        bb.scale = (ancho * 0.95, 0.75, 0.75)

    return {"pelo": estilo_pelo, "lentes": tiene_lentes, "vello": vello}


# ----------------------------------------------------------------- render ---
def preparar_render():
    esc = bpy.context.scene
    esc.render.engine = 'BLENDER_WORKBENCH'
    sh = esc.display.shading
    sh.light = 'FLAT'
    sh.color_type = 'OBJECT'
    sh.show_object_outline = True
    sh.object_outline_color = (0.09, 0.07, 0.06)
    esc.display.render_aa = '8'
    esc.render.resolution_x = 512
    esc.render.resolution_y = 512
    esc.render.film_transparent = False
    esc.world = bpy.data.worlds.new("Mundo")
    esc.world.color = FONDO

    cam_data = bpy.data.cameras.new("Cam")
    cam = bpy.data.objects.new("Cam", cam_data)
    bpy.context.collection.objects.link(cam)
    cam.location = (0, -6.2, 1.35)
    cam.rotation_euler = (math.pi / 2 * 0.97, 0, 0)
    cam_data.lens = 65
    bpy.context.scene.camera = cam


def render_a(ruta):
    bpy.context.scene.render.filepath = ruta
    bpy.ops.render.render(write_still=True)


# ------------------------------------------------------------------- main ---
def main():
    manifiesto = []
    for i in range(CANTIDAD):
        for variante in ("", "_alt"):
            limpiar_escena()
            rng = random.Random(20260724 + i)  # misma semilla estructural
            rasgos = construir_busto(rng, alt=(variante == "_alt"))
            preparar_render()
            nombre = f"retrato_{i:03d}{variante}.png"
            render_a(os.path.join(SALIDA, nombre))
            if variante == "":
                manifiesto.append(f"{i:03d}|{rasgos['pelo']}|{int(rasgos['lentes'])}|{rasgos['vello']}")
        print(f"PROGRESO {i + 1}/{CANTIDAD}", flush=True)

    with open(os.path.join(SALIDA, "manifiesto.txt"), "w", encoding="utf-8") as f:
        f.write("\n".join(manifiesto))
    print(f"RETRATOS_OK {CANTIDAD * 2} renders en {SALIDA}", flush=True)


main()
