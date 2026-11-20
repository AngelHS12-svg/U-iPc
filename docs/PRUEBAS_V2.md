# Verificación de U-iPC 2

Fecha de ejecución: 19 de noviembre de 2026.

## Pruebas del modelo

Los fuentes del juego y del editor compilaron contra las bibliotecas instaladas de Unity 6000.0.37f1.

- 67 comprobaciones de estados de piezas aprobadas.
- 309 comprobaciones de campaña aprobadas.
- Total: 376 comprobaciones del modelo.

Las pruebas recorren los doce contratos desde un perfil nuevo, con su saldo inicial y las compras necesarias. Verifican desbloqueos, montaje, diagnóstico, compatibilidad, presupuesto, recompensas únicas, guardado parcial, errores, vencimiento del plazo y restauración de compras al reintentar.

## Unity y ejecutable

El recorrido automático dentro de Unity aprobó los doce contratos y generó capturas de los tres escenarios. También se compiló una versión de Windows sin errores y su proceso de verificación terminó con código de salida 0 y doce contratos aprobados.

Los resultados del modelo no sustituyen las pruebas del motor. El flujo se comprueba en ambos niveles. El recorrido automático usa el controlador y las acciones del simulador, sin modificar la carrera guardada del jugador.

## Aspectos por medir

No se asigna un resultado aprobado de FPS, consumo de memoria, accesibilidad o usabilidad sin una medición específica. Una futura entrega puede incorporar una bitácora con esos datos y pruebas con más jugadores.

## Reproducción

Ejecuta los proyectos de pruebas con .NET SDK 10 o usa VERIFICAR_CODIGO.ps1 con las bibliotecas de Unity. Para el motor, utiliza PC FIX LAB, Verificar recorridos y capturas, o VERIFICAR_JUEGO.cmd sobre una compilación.
