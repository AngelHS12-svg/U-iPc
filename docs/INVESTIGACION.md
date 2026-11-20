# Investigación aplicada a U-iPC 2

Consulta realizada el 19 de noviembre de 2026. Las reglas se inspiran en procedimientos publicados por fabricantes; los escenarios y valores numéricos son decisiones de diseño del juego.

## Arranque y descarte

Intel distingue problemas de alimentación, arranque y vídeo. El simulador evalúa fuente, montaje, memoria, vídeo, almacenamiento y temperatura en un orden definido. Un síntoma no identifica por sí solo una pieza defectuosa. Se evita presentar códigos sonoros como universales.

Fuente: [Intel No Boot or No Display](https://www.intel.com/content/www/us/en/support/articles/000021605/processors.html).

## Conexiones y almacenamiento

La documentación de Intel sobre unidades no detectadas contempla las conexiones además de una posible avería. Por eso SMART no confirma una avería interna mientras hay un cable flojo. El jugador debe corregirlo y repetir la prueba.

Fuente: [Intel SSD Is Not Detected in BIOS](https://www.intel.com/content/www/us/en/support/articles/000060033/memory-and-storage.html).

## Temperatura

Dell relaciona polvo, flujo de aire, ventiladores y material térmico con problemas de temperatura. Esto se adapta a mantenimiento por etapas y comprobación bajo carga. La fórmula del juego es sencilla y no estima la temperatura real de una PC ni representa todas sus posibles causas de sobrecalentamiento.

Fuente: [Dell Overheating and Shutdown Troubleshooting](https://www.dell.com/support/kbdoc/en-us/000130867/how-to-troubleshoot-a-overheating-shutdown-or-thermal-issue-on-a-dell-pc).

## Compatibilidad

Kingston diferencia formato, interfaz y protocolo en almacenamiento. El banco del juego usa SATA y bloquea un SSD NVMe. También se comprueban generación DDR, socket de disipador y capacidad y potencia mínimas indicadas por el contrato.

Fuente: [Kingston SATA and M.2 FAQs](https://www.kingston.com/en/ssd/ssd-faq).

La potencia mínima es un requisito ficticio del caso, no una calculadora del consumo de componentes reales. Se usan nombres genéricos.

## Decisiones de simulación

- Tres escenarios reutilizan cámara y estación para limitar el consumo de recursos.
- Los contratos progresan de una falla a varias simultáneas.
- Aprendiz permite experimentar; los niveles posteriores añaden tiempo y consecuencias.
- Compatibilidad, economía y seguridad se validan en el modelo además de en los botones.
- Reintentar restaura las compras para evitar bloquear permanentemente una partida.
- Se prueban las reglas sin interfaz y después se recorre la campaña dentro de Unity.

Soldadura, recuperación de datos, BIOS y sistemas operativos virtuales requieren mecánicas y pruebas adicionales. No son funciones de esta versión.
