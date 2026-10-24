# ScreenMover

**Tus ventanas a la vista. Arrástralas a otra pantalla u ocúltalas.** Una herramienta visual para organizar ventanas en varios monitores de Windows.

[简体中文](README.md) · [English](README.en.md) · [Deutsch](README.de.md) · [Français](README.fr.md) · [Español](README.es.md)

[Última versión](https://github.com/lbyh0116/ScreenMover/releases/latest) · [Informar de un problema](https://github.com/lbyh0116/ScreenMover/issues/new/choose)

## Descargar y ejecutar

Versión estable actual: **v0.2.2 · Windows x64, portátil**.

1. Descarga [ScreenMover-v0.2.2-win-x64.zip](https://github.com/lbyh0116/ScreenMover/releases/download/v0.2.2/ScreenMover-v0.2.2-win-x64.zip).
2. Extrae todo el archivo y ejecuta `ScreenMover.exe`. Mantén juntos todos los archivos incluidos.
3. Antes de actualizar, cierra la versión anterior desde su icono en el área de notificación. Se conservan los ajustes, el orden de las ventanas y los registros de recuperación.

Se incluyen los componentes necesarios; no hace falta instalar .NET por separado. El paquete no tiene firma de código comercial. La edición de Microsoft Store todavía no está disponible. La interfaz admite chino simplificado, inglés, alemán, francés y español. Por defecto sigue el idioma de Windows; si no es compatible, utiliza inglés. Puedes cambiar el idioma en los ajustes y reiniciar para aplicarlo.

## Funciones

- **Vista general:** miniaturas estáticas de pantallas y ventanas en una galería. Si no se puede capturar una ventana, se muestra el icono de su aplicación.
- **Arrastrar entre pantallas:** lleva las ventanas a la tarjeta del monitor de destino. Admite selección múltiple, movimientos en grupo y deshacer el último movimiento.
- **Panel rápido:** una lista compacta de ventanas junto a las vistas previas de los monitores, accesible desde el área de notificación. Se cierra al hacer clic fuera.
- **Ocultar y recuperar ventanas:** en las compilaciones de Windows compatibles, mueve una ventana a un escritorio virtual independiente con el nombre de su aplicación e intenta minimizarla. Puedes recuperarla desde la ventana principal. Si la aplicación no permite minimizarse, se mantiene el cambio de escritorio ya realizado. Los movimientos manuales de vuelta se detectan automáticamente.
- **Personalización:** orden de ventanas guardado, modo claro u oscuro, color de énfasis del sistema o personalizado, atajos configurables y guardado automático de los ajustes.
- **Actualizaciones desde la aplicación:** consulta GitHub Releases, descarga y verifica el paquete correspondiente, y después instala y reinicia.

Las vistas previas son capturas puntuales, no una grabación continua. Por defecto se actualizan al iniciar, al volver a activar ScreenMover, cada minuto o al pulsar F5. Al cerrar la ventana principal, la aplicación normalmente sigue en el área de notificación. Usa el menú de su icono para salir por completo.

## Controles

| Acción | Cómo hacerlo |
| --- | --- |
| Mover una ventana | Arrastrarla a la tarjeta de otra pantalla o usar los botones de movimiento |
| Seleccionar varias / un intervalo | Ctrl / Mayús + clic |
| Activar una ventana | Doble clic en su tarjeta |
| Buscar / seleccionar todo | Ctrl+F / Ctrl+A |
| Actualizar vistas previas | F5 |
| Cancelar / cerrar el panel rápido | Esc; hacer clic en un espacio vacío borra la selección |
| Abrir el panel rápido / ocultar la ventana activa | Configurar atajos globales distintos en los ajustes |

En el panel rápido, soltar una ventana sobre su pantalla actual la oculta; soltarla sobre otra pantalla la mueve. Una indicación sobre el destino muestra qué ocurrirá antes de soltarla.

## Compatibilidad

- **Windows 11 x64:** utiliza Mica cuando está disponible. La ocultación en un escritorio virtual independiente solo está habilitada en las compilaciones **26100 / 26200**.
- **Windows 10 22H2 x64:** compatibilidad básica prevista, con Acrílico cuando el sistema lo permite. Sigue pendiente la validación en equipos con Windows 10. La ocultación en un escritorio virtual independiente está deshabilitada.
- **x86 / ARM64:** todavía no hay paquetes publicados.

Las ventanas protegidas, minimizadas, dibujadas mediante GPU o que no responden pueden no ofrecer una vista previa. Para gestionar aplicaciones con permisos elevados puede ser necesario reiniciar ScreenMover como administrador. Las configuraciones con distintos valores de DPI, la conexión de monitores en caliente y los diferentes dispositivos gráficos necesitan más pruebas. [Detalles de compatibilidad](docs/COMPATIBILITY.md) (en chino).

## Actualizaciones y datos locales

Desde los ajustes, busca actualizaciones y elige descargar e instalar. Las descargas se verifican con SHA-256. Si falla la sustitución de archivos, se intenta restaurar la versión anterior; no se garantiza la recuperación automática tras un corte de corriente o un fallo posterior de la nueva versión. Las comprobaciones las inicia el usuario, no se realizan en segundo plano. [Detalles de las actualizaciones](docs/UPDATES.md) (en chino).

Los datos locales se guardan en `%LOCALAPPDATA%\ScreenMover`. Las capturas permanecen en memoria y no se suben a internet. Los registros de actividad y recuperación pueden incluir títulos de ventanas. Las copias de seguridad de las actualizaciones se guardan en la subcarpeta `updates`. [Privacidad](docs/PRIVACY.md) (en chino).

## Compilar desde el código fuente

Requisitos: Windows x64, **SDK de .NET 10.0.400** y acceso a NuGet.

```powershell
git clone https://github.com/lbyh0116/ScreenMover.git
cd ScreenMover
.\build.ps1 -PureTest
```

Resultado: `dist\ScreenMover\ScreenMover.exe`. El comando compila el proyecto, prepara el paquete y ejecuta pruebas de regresión sin interfaz, sin manipular tus ventanas. [Hoja de ruta](docs/ROADMAP.md) · [Pruebas manuales](docs/MANUAL_TEST.md) (en chino).

## Licencia y seguridad

Todavía no se ha elegido una licencia de código abierto para el código original. Que el código sea público no implica que se conceda una licencia de código abierto. El código auxiliar de los escritorios virtuales procede de [MScholtes/VirtualDesktop](https://github.com/MScholtes/VirtualDesktop) y conserva su [licencia MIT](https://github.com/lbyh0116/ScreenMover/blob/main/ScreenMover/Helpers/VirtualDesktop-LICENSE.txt). Las demás dependencias mantienen sus respectivas licencias.

Para comunicar una vulnerabilidad, utiliza el [informe privado](https://github.com/lbyh0116/ScreenMover/security/advisories/new). Para otros problemas, abre una issue en GitHub. [Notas de la última versión](CHANGELOG.md) (en chino e inglés).
