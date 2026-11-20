# U-iPC 2.0

Simulador 3D de reparación de computadoras para Windows, desarrollado en Unity 6 y C#. Autora: **Lizbeth Valerio Garcia**.

El jugador administra un taller, diagnostica equipos, elige recambios compatibles y entrega reparaciones verificadas. La cámara gira alrededor de una mesa. Funciona localmente, sin servicios de IA durante las partidas.

## Novedades

- **12 contratos y tres escenarios:** taller de barrio, laboratorio universitario y servicio profesional.
- Tres dificultades: aprendiz, técnica y especialista.
- RAM, SSD, refrigeración, fuente y GPU manipulables.
- Averías internas, cables flojos, piezas mal asentadas, polvo y pasta deteriorada, con fallas combinadas.
- Inspección visual y cinco instrumentos específicos de banco.
- Tienda con doce recambios, inventario, presupuesto y compatibilidad de interfaz, capacidad y potencia.
- Arranque según el estado del equipo, prueba de estabilidad y entrega explícita.
- Calidad, estrellas, créditos, experiencia y reputación.
- Guardado del perfil y de la reparación en curso.
- Bitácora, pistas, pausa y reintentos con restauración de compras.

Los escenarios reutilizan la estación y se diferencian en mobiliario, materiales y ambientación; no son mapas de exploración libre.

## Abrir y jugar

1. Instala Unity **6000.0.37f1**, con soporte de compilación para Windows.
2. Agrega la carpeta **PCRepairLab** desde Unity Hub.
3. Espera a que termine de importar y compilar.
4. Elige **PC FIX LAB → 2 - Jugar en Unity**, abre Game y acepta un contrato.

También puedes ejecutar ABRIR_EN_UNITY.cmd. Para otra instalación de Unity, define UIPC_EDITOR con la ruta de Unity.exe o abre desde el Hub. El archivo opcional proyecto-activo.local.txt permite indicar otra copia; no se incluye en Git.

## Controles

| Control | Acción |
| --- | --- |
| Clic izquierdo | Seleccionar una pieza o botón |
| Botón derecho y arrastrar | Girar cámara sobre la escena |
| Rueda del ratón | Acercar o alejar sobre la escena |
| R | Restablecer vista |
| H | Ayuda |
| Esc | Pausa o cerrar un panel |

Los accesos RAM, SSD, CPU/FAN, FUENTE, GPU y TAPA facilitan la selección.

## Ciclo de una reparación

1. Observa el síntoma con ENCENDER Y PROBAR.
2. Apaga, desconecta y abre.
3. Inspecciona y selecciona una prueba en DIAGNÓSTICO. Una conexión floja puede producir un resultado inconcluso.
4. Reasienta, reconecta o realiza mantenimiento. Sustituye sólo tras confirmar una avería interna.
5. Para sustituir: retira, abre TIENDA / STOCK, compra un recambio compatible e instálalo.
6. Monta todo, cierra, conecta y enciende.
7. En contratos avanzados, comprueba estabilidad.
8. Pulsa ENTREGAR EQUIPO para registrar el pago.

Consulta [el manual de la versión 2](docs/MANUAL_V2.md).

## Dificultad

| Escenario | Contratos | Plazo | Errores máximos |
| --- | --- | --- | --- |
| Taller de barrio | 1 a 4 | Sin límite obligatorio | Práctica ilimitada |
| Laboratorio universitario | 5 a 8 | 12 minutos de juego | 6 |
| Servicio profesional | 9 a 12 | 10 minutos de juego | 4 |

Inspeccionar añade 5 segundos; probar una pieza, 25; comprobar estabilidad, 45. El trabajo en la escena consume tiempo real. Los paneles detienen el reloj real. Una pista avanzada cuesta 30 segundos y un error.

Reintentar restaura el saldo y stock del inicio del contrato; al reiniciar un fallo, la reputación baja 5 puntos. No hay deuda. Repetir un contrato terminado es un nuevo servicio remunerado; una misma entrega no puede cobrarse dos veces.

## Guardado

Se guarda después de las acciones relevantes, cada diez segundos de juego y al salir. Se conservan piezas retiradas, averías, compras, stock, tiempo, errores y bitácora. El formato v2 tiene su propia clave; se migra el avance inicial compatible cuando existe.

Nueva carrera requiere confirmación y reinicia el perfil. Un guardado inválido se conserva como copia antes de iniciar otro.

## Compilar

Detén Play Mode y elige **PC FIX LAB → 3 - Compilar para Windows**.

Resultado: **Builds/U-iPC/U-iPC.exe**. Distribuye la carpeta completa con sus datos y bibliotecas.

Con el editor cerrado también puedes usar COMPILAR_WINDOWS.cmd.

## Pruebas

Con .NET SDK 10, ejecuta:

    dotnet run --project Tests/PartStateTests.csproj --configuration Release
    dotnet run --project Tests/CampaignTests.csproj --configuration Release

VERIFICAR_CODIGO.ps1 compila también los fuentes contra las bibliotecas de Unity instaladas.

En el editor: **PC FIX LAB → 4 - Verificar recorridos y capturas**. Mantén visible la pestaña Game. El verificador no sustituye el perfil del jugador. Tras compilar puedes usar VERIFICAR_JUEGO.cmd.

### Evidencia disponible

- Fuentes de ejecución y editor compilados contra Unity 6000.0.37f1.
- 67 comprobaciones de piezas y 309 comprobaciones de campaña aprobadas.
- Recorrido dentro de Unity: **12 contratos aprobados**, con capturas de los tres escenarios.
- Ejecutable Windows 2.0: **12 contratos aprobados**, salida correcta y renderizado comprobado.
- FPS y memoria deben medirse en el ejecutable; un objetivo de rendimiento no equivale a un resultado medido.

GitHub Actions ejecuta las dos suites del modelo sin una licencia de Unity. El resultado remoto se consulta en Actions.

## Estructura

| Archivo | Responsabilidad |
| --- | --- |
| PartState.cs | Estados y transiciones de una pieza |
| CampaignModel.cs | Contratos, economía, compatibilidad, sesiones y guardados |
| RepairDomain.cs | Componentes interactivos de Unity |
| WorkshopWorld.cs | Modelos y escenarios originales |
| RepairGame.cs | Interfaz, entradas y coordinación |
| BuildAutomation.cs | Compilación Windows |
| Tests | Pruebas reproducibles del modelo |

## Alcance

Simulación por reglas, no emulación eléctrica ni herramienta para diagnosticar equipos reales. Temperaturas, precios y duraciones son valores del juego. No se simulan recuperación de datos, soldadura, apertura de fuentes, instalación de sistemas ni actualización real de BIOS.

Los modelos, materiales y sonidos se generan en Unity. Las fuentes del sistema se cargan en tiempo de ejecución. Consulta [la investigación](docs/INVESTIGACION.md), [los cambios](CHANGELOG.md) y [los créditos](CREDITOS.md).
