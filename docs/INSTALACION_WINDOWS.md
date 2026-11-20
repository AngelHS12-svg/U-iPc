# Instalar y jugar U-iPC 2.0 en Windows

## Descargar el juego

Abre [la descarga de U-iPC 2.0](https://github.com/AngelHS12-svg/U-iPc/releases/tag/v2.0.0). En **Assets**, descarga **U-iPC_2_Windows.zip**. Ese paquete contiene el juego compilado, incluido **U-iPC.exe** y sus archivos necesarios.

Los archivos **Source code (zip)** y **Source code (tar.gz)** contienen el proyecto para desarrolladores. Para jugar utiliza el paquete Windows.

## Requisitos

- Windows de 64 bits, con gráficos compatibles con DirectX 11.
- Teclado y ratón con rueda para manejar la cámara.
- Reserva aproximadamente 250 MB libres para descargar y extraer el paquete.
- Para jugar no necesitas instalar Unity, Blender ni .NET SDK. El juego funciona sin conexión después de descargarlo.

La compilación se probó en Windows de 64 bits. No se han establecido mínimos de RAM o GPU mediante una prueba de rendimiento en varios equipos.

## Preparación y primer inicio

1. Busca **U-iPC_2_Windows.zip** en Descargas.
2. Haz clic derecho y selecciona **Extraer todo**.
3. Elige una carpeta de tu usuario donde puedas escribir, por ejemplo **Documentos\Juegos**.
4. Abre la carpeta extraída **U-iPC**.
5. Haz doble clic en **U-iPC.exe**.
6. En el mapa de carrera, acepta **Primer arranque** para comenzar. Los siguientes contratos se desbloquean al entregar el anterior.

Esta versión es portable: se prepara extrayendo el ZIP y se inicia con el ejecutable. No incluye un asistente Setup. Puedes crear un acceso directo a U-iPC.exe para abrirlo desde el escritorio.

## Conserva la carpeta completa

Junto a U-iPC.exe deben permanecer **U-iPC_Data**, **UnityPlayer.dll** y **MonoBleedingEdge**, además del resto de archivos distribuidos. No muevas únicamente el .exe ni lo ejecutes desde el interior del ZIP.

El paquete también incluye **MANUAL_USUARIO.md** y esta guía. Puedes consultar [el manual en GitHub](MANUAL_V2.md).

## Controles y primera reparación

| Control | Función |
| --- | --- |
| Clic izquierdo | Seleccionar piezas y botones |
| Botón derecho y arrastrar sobre la escena | Girar alrededor de la mesa |
| Rueda sobre la escena | Acercar y alejar |
| R | Restablecer cámara |
| H | Mostrar ayuda |
| Esc | Pausar o cerrar un panel |

En el primer contrato: observa el arranque, apaga, desconecta, abre, diagnostica la RAM, retírala e instala la unidad del inventario. Después cierra, conecta, comprueba el arranque y entrega el equipo.

## Guardar, actualizar y retirar el juego

La carrera y el encargo activo se guardan automáticamente después de las acciones relevantes, periódicamente durante el trabajo y al cerrar normalmente. Abre el juego con el mismo usuario de Windows para continuar. El recorrido automatizado de pruebas usa un perfil separado.

Para actualizar, cierra el juego y extrae la nueva versión en otra carpeta completa. No mezcles archivos de compilaciones distintas. La continuidad del guardado depende de que la nueva versión conserve un formato compatible.

Para retirar esta versión portable, cierra el juego y elimina su carpeta. El progreso se almacena por separado en el perfil de Windows; dentro del juego, **Nueva carrera** permite reiniciarlo tras confirmar.

## Problemas frecuentes

| Problema | Qué revisar |
| --- | --- |
| Falta UnityPlayer.dll, Mono o la carpeta Data | Extrae otra vez el paquete completo y conserva la estructura original. |
| Se abre Unity o sólo aparecen fuentes C# | Descargaste los fuentes; utiliza U-iPC_2_Windows.zip desde Assets. |
| No inicia por falta de compatibilidad gráfica | Comprueba que Windows sea de 64 bits y la GPU sea compatible con DirectX 11; usa el controlador oficial de tu GPU. |
| La cámara no gira | Arrastra con el botón derecho sobre la escena central; los paneles capturan el ratón. |
| Los niveles aparecen bloqueados | Es la progresión normal: termina y entrega el contrato anterior. |
| No permite entregar el equipo | Monta todas las piezas, cierra, conecta, verifica el arranque y, en niveles avanzados, la estabilidad. |

Si el problema continúa, abre una incidencia en el repositorio e indica el síntoma, la versión y los pasos para reproducirlo. El registro de Unity suele encontrarse en **%USERPROFILE%\AppData\LocalLow\Lizbeth Valerio Garcia\U-iPC\Player.log**. Revisa su contenido antes de compartirlo y omite datos personales.

## Capturas

Consulta [la galería de novedades](CAPTURAS_V2.md) para ver la campaña y los escenarios de la versión 2.
