# SmartRetailAPP

Aplicación de gestión para retail que permite manejar clientes, productos, ventas y detalle de ventas utilizando PostgreSQL como base de datos en la nube.

---

## Descripción

SmartRetailAPP es una solución sencilla para la administración de tiendas minoristas, que facilita:

- Gestión de clientes con datos básicos (nombre, correo, teléfono).
- Gestión de productos con stock y precios.
- Registro de ventas vinculadas a clientes.
- Detalle de las ventas con cantidades y subtotales.

La base de datos está diseñada con PostgreSQL y utiliza UUID para identificar de forma única los registros.

---

## Tecnologías

- PostgreSQL (con extensión `uuid-ossp`)
- SQL para la creación y manipulación de tablas y datos
- Git para control de versiones

---

## Estructura de la base de datos

- **Cliente:** Tabla con información de clientes.
- **Producto:** Tabla con detalles de productos.
- **Venta:** Registro de ventas, con referencia a clientes.
- **Detalle_Venta:** Detalle de productos en cada venta.

---

## Cómo usar

1. Clona el repositorio:
   ```bash
   git clone https://github.com/Alvader01/SmartRetailAPP.git
   ```

2. Ejecuta los scripts SQL para crear las tablas y cargar datos iniciales en PostgreSQL.

3. Modifica y adapta el proyecto según tus necesidades.

---

## Contribuciones

¡Bienvenidas! Si quieres colaborar, por favor crea un fork y envía un pull request con tus mejoras o correcciones.

---

## Licencia

Este proyecto está bajo la licencia MIT. Consulta el archivo `LICENSE` para más detalles.

---

## Contacto

Para dudas o sugerencias, puedes contactarme vía GitHub: [@Alvader01](https://github.com/Alvader01) o via Discord alvader
