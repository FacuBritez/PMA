import os
import threading
import time
import tkinter as tk
from tkinter import messagebox
from PIL import Image, ImageTk
from queue import Queue
import datetime
import io

CARPETA_ORIGEN = "imagenes temporal"
CARPETA_DESTINO = "WatchedImages"
EXTENSIONES = ('.png', '.jpg', '.jpeg', '.bmp')
POLL_INTERVAL = 1.0

os.makedirs(CARPETA_ORIGEN, exist_ok=True)
os.makedirs(CARPETA_DESTINO, exist_ok=True)


# ── WIA via pywin32 ────────────────────────────────────────────────────────────

def listar_escaners_wia():
    import win32com.client
    import pythoncom
    pythoncom.CoInitialize()
    mgr = win32com.client.Dispatch("WIA.DeviceManager")
    return [(info.Properties["Name"].Value, info.DeviceID)
            for info in mgr.DeviceInfos if info.Type == 1]


def escanear_wia(device_id, dpi=300):
    import win32com.client
    import pythoncom
    pythoncom.CoInitialize()
    mgr = win32com.client.Dispatch("WIA.DeviceManager")
    device = None
    for info in mgr.DeviceInfos:
        if info.DeviceID == device_id:
            device = info.Connect()
            break
    if device is None:
        raise RuntimeError("No se pudo conectar al escáner.")

    item = device.Items[1]

    def set_prop(prop_id, value):
        try:
            item.Properties[prop_id].Value = value
        except Exception:
            pass

    set_prop(6146, 1)    # Intent: Color
    set_prop(6147, dpi)  # H-resolution
    set_prop(6148, dpi)  # V-resolution

    wia_image = item.Transfer("{B96B3CAE-0728-11D3-9D7B-0000F81EF32E}")  # BMP GUID
    raw_bytes = bytes(wia_image.FileData.BinaryData)
    imagen = Image.open(io.BytesIO(raw_bytes))
    imagen.load()
    return imagen


# ── App ────────────────────────────────────────────────────────────────────────

class EditorImagenes:
    def __init__(self, root):
        self.root = root
        self.root.title("Editor de Escaneos - con Escáner")
        self.root.geometry("900x700")

        self.imagen_original = None
        self.imagen_actual = None
        self.tk_image = None
        self.historial = []
        self.rectangulo = None
        self.rect_start = None
        self.rect_end = None
        self.escala_x = 1.0
        self.escala_y = 1.0
        self.offset_x = 0
        self.offset_y = 0
        self.archivo_actual = None
        self.cola_imagenes = Queue()
        self.monitor_activo = True

        # Detectar escáneres al inicio
        self._escaners = []
        threading.Thread(target=self._detectar_escaners, daemon=True).start()

        frame_botones = tk.Frame(root)
        frame_botones.pack(side=tk.TOP, fill=tk.X, padx=5, pady=5)

        tk.Button(frame_botones, text="Girar 90°",
                  command=self.girar).pack(side=tk.LEFT, padx=2)
        tk.Button(frame_botones, text="Espejar H",
                  command=lambda: self.espejar('h')).pack(side=tk.LEFT, padx=2)
        tk.Button(frame_botones, text="Espejar V",
                  command=lambda: self.espejar('v')).pack(side=tk.LEFT, padx=2)
        tk.Button(frame_botones, text="Aplicar recorte",
                  command=self.aplicar_recorte).pack(side=tk.LEFT, padx=2)
        tk.Button(frame_botones, text="Deshacer",
                  command=self.deshacer).pack(side=tk.LEFT, padx=2)
        tk.Button(frame_botones, text="Guardar y siguiente",
                  command=self.guardar_y_siguiente).pack(side=tk.LEFT, padx=2)

        self.btn_escanear = tk.Button(frame_botones, text="Escanear",
                                      command=self.escanear)
        self.btn_escanear.pack(side=tk.LEFT, padx=2)

        self.lbl_estado = tk.Label(frame_botones, text="Buscando escáneres…",
                                   fg="gray")
        self.lbl_estado.pack(side=tk.LEFT, padx=8)

        self.canvas = tk.Canvas(root, bg='gray', cursor="cross")
        self.canvas.pack(fill=tk.BOTH, expand=True, padx=5, pady=5)

        self.canvas.bind("<ButtonPress-1>", self.on_press)
        self.canvas.bind("<B1-Motion>", self.on_drag)
        self.canvas.bind("<ButtonRelease-1>", self.on_release)

        threading.Thread(target=self.monitorear_carpeta, daemon=True).start()
        self.procesar_cola()

    # ── Detección de escáneres ─────────────────────────────────────────────────

    def _detectar_escaners(self):
        try:
            devs = listar_escaners_wia()
            self._escaners = devs
            if devs:
                nombres = ", ".join(n for n, _ in devs)
                msg = f"Escáner: {nombres}"
            else:
                msg = "No se detectaron escáneres"
        except Exception as exc:
            self._escaners = []
            msg = f"Error al buscar escáneres: {exc}"
        self.root.after(0, lambda: self.lbl_estado.config(text=msg))

    # ── Escaneo ────────────────────────────────────────────────────────────────

    def escanear(self):
        if not self._escaners:
            messagebox.showwarning(
                "Sin escáner",
                "No se detectó ningún escáner.\n\n"
                "Asegurate de tener pywin32 instalado:\n"
                "  pip install pywin32"
            )
            return

        # Si hay más de un escáner, usar el primero (se puede extender con un selector)
        _, device_id = self._escaners[0]

        self.btn_escanear.config(state="disabled")
        self.lbl_estado.config(text="Escaneando…")

        def run():
            try:
                imagen = escanear_wia(device_id, dpi=300)

                timestamp = datetime.datetime.now().strftime("%Y%m%d_%H%M%S")
                nombre_salida = f"scan_{timestamp}.png"
                ruta_destino = os.path.join(CARPETA_ORIGEN, nombre_salida)
                imagen.save(ruta_destino)

                self.root.after(0, lambda: self._scan_ok(ruta_destino, nombre_salida))
            except Exception as exc:
                self.root.after(0, lambda e=exc: self._scan_error(str(e)))

        threading.Thread(target=run, daemon=True).start()

    def _scan_ok(self, ruta, nombre):
        self.btn_escanear.config(state="normal")
        self.lbl_estado.config(text=f"Escaneado: {nombre}")
        self.archivo_actual = None  # liberar para que se pueda cargar
        self.cargar_imagen(ruta)

    def _scan_error(self, msg):
        self.btn_escanear.config(state="normal")
        self.lbl_estado.config(text="Error al escanear")
        messagebox.showerror("Error de escaneo", msg)

    # ── Monitor de carpeta ─────────────────────────────────────────────────────

    def monitorear_carpeta(self):
        ya_vistas = set()
        while self.monitor_activo:
            try:
                archivos = [f for f in os.listdir(CARPETA_ORIGEN)
                            if f.lower().endswith(EXTENSIONES)]
                for f in archivos:
                    ruta = os.path.join(CARPETA_ORIGEN, f)
                    if ruta not in ya_vistas and ruta != self.archivo_actual:
                        ya_vistas.add(ruta)
                        self.cola_imagenes.put(ruta)
            except Exception:
                pass
            time.sleep(POLL_INTERVAL)

    def procesar_cola(self):
        if self.archivo_actual is None and not self.cola_imagenes.empty():
            siguiente = self.cola_imagenes.get()
            self.cargar_imagen(siguiente)
        self.root.after(500, self.procesar_cola)

    # ── Carga y edición ────────────────────────────────────────────────────────

    def cargar_imagen(self, ruta):
        try:
            self.archivo_actual = ruta
            self.imagen_original = Image.open(ruta)
            self.imagen_actual = self.imagen_original.copy()
            self.historial = []
            self.rectangulo = None
            self.rect_start = None
            self.rect_end = None
            self.actualizar_canvas()
        except Exception as e:
            messagebox.showerror("Error", f"No se pudo cargar {ruta}\n{e}")
            self.archivo_actual = None

    def guardar_y_siguiente(self):
        if self.archivo_actual is None:
            return
        nombre = os.path.basename(self.archivo_actual)
        destino = os.path.join(CARPETA_DESTINO, nombre)
        try:
            self.imagen_actual.save(destino)
            os.remove(self.archivo_actual)
            self.archivo_actual = None
            self.imagen_actual = None
            self.imagen_original = None
            self.canvas.delete("all")
            self.tk_image = None
            self.procesar_cola()
        except Exception as e:
            messagebox.showerror("Error", f"No se pudo guardar: {e}")

    def girar(self):
        if self.imagen_actual is None:
            return
        self.guardar_historial()
        self.imagen_actual = self.imagen_actual.rotate(90, expand=True)
        self.actualizar_canvas()

    def espejar(self, direccion):
        if self.imagen_actual is None:
            return
        self.guardar_historial()
        if direccion == 'h':
            self.imagen_actual = self.imagen_actual.transpose(Image.FLIP_LEFT_RIGHT)
        else:
            self.imagen_actual = self.imagen_actual.transpose(Image.FLIP_TOP_BOTTOM)
        self.actualizar_canvas()

    def guardar_historial(self):
        if self.imagen_actual:
            self.historial.append(self.imagen_actual.copy())

    def deshacer(self):
        if not self.historial:
            return
        self.imagen_actual = self.historial.pop()
        self.actualizar_canvas()
        if self.rectangulo:
            self.canvas.delete(self.rectangulo)
            self.rectangulo = None

    def aplicar_recorte(self):
        if self.imagen_actual is None or self.rect_end is None or self.rect_start is None:
            return
        x1 = min(self.rect_start[0], self.rect_end[0])
        y1 = min(self.rect_start[1], self.rect_end[1])
        x2 = max(self.rect_start[0], self.rect_end[0])
        y2 = max(self.rect_start[1], self.rect_end[1])

        ix1 = int((x1 - self.offset_x) / self.escala_x)
        iy1 = int((y1 - self.offset_y) / self.escala_y)
        ix2 = int((x2 - self.offset_x) / self.escala_x)
        iy2 = int((y2 - self.offset_y) / self.escala_y)

        ix1 = max(0, ix1)
        iy1 = max(0, iy1)
        ix2 = min(self.imagen_actual.width, ix2)
        iy2 = min(self.imagen_actual.height, iy2)

        if ix2 <= ix1 or iy2 <= iy1:
            return

        self.guardar_historial()
        self.imagen_actual = self.imagen_actual.crop((ix1, iy1, ix2, iy2))
        self.actualizar_canvas()
        if self.rectangulo:
            self.canvas.delete(self.rectangulo)
            self.rectangulo = None
            self.rect_start = None
            self.rect_end = None

    def actualizar_canvas(self):
        if self.imagen_actual is None:
            return
        canvas_width = self.canvas.winfo_width()
        canvas_height = self.canvas.winfo_height()
        if canvas_width <= 1 or canvas_height <= 1:
            canvas_width, canvas_height = 800, 600

        ancho_img, alto_img = self.imagen_actual.size
        self.escala_x = min(canvas_width / ancho_img, canvas_height / alto_img)
        self.escala_y = self.escala_x
        nuevo_ancho = int(ancho_img * self.escala_x)
        nuevo_alto = int(alto_img * self.escala_y)
        self.offset_x = (canvas_width - nuevo_ancho) // 2
        self.offset_y = (canvas_height - nuevo_alto) // 2

        img_resized = self.imagen_actual.resize((nuevo_ancho, nuevo_alto),
                                                Image.Resampling.LANCZOS)
        self.tk_image = ImageTk.PhotoImage(img_resized)
        self.canvas.delete("all")
        self.canvas.create_image(self.offset_x, self.offset_y,
                                  anchor=tk.NW, image=self.tk_image)

    def on_press(self, event):
        if self.imagen_actual is None:
            return
        self.rect_start = (event.x, event.y)
        if self.rectangulo:
            self.canvas.delete(self.rectangulo)
            self.rectangulo = None

    def on_drag(self, event):
        if self.rect_start is None:
            return
        if self.rectangulo:
            self.canvas.delete(self.rectangulo)
        x1, y1 = self.rect_start
        self.rect_end = (event.x, event.y)
        self.rectangulo = self.canvas.create_rectangle(
            x1, y1, event.x, event.y, outline='red', width=2)

    def on_release(self, event):
        if self.rect_start is None:
            return
        self.rect_end = (event.x, event.y)


if __name__ == "__main__":
    root = tk.Tk()
    app = EditorImagenes(root)
    root.mainloop()