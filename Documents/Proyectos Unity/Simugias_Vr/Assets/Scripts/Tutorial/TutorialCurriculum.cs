/// <summary>
/// Currículum por defecto del simulador de suturectomía endoscópica.
/// Si el director no tiene módulos asignados en el Inspector, usa este catálogo.
/// </summary>
public static class TutorialCurriculum
{
    public static TutorialModuleConfig[] Build()
    {
        return new[]
        {
            Intro(),
            Interaction(),
            Instruments(),
            Anatomy(),
            Procedure(),
            Finish()
        };
    }

    static TutorialModuleConfig Intro()
    {
        return new TutorialModuleConfig
        {
            id = "intro",
            title = "Introducción",
            description = "Mirar, manos y botones básicos.",
            steps = new[]
            {
                S("intro.look", "Bienvenida",
                    "Gire la cabeza a izquierda y derecha. El panel lo sigue.",
                    TutorialTargetKind.None, TutorialCompleteWhen.LookedAround,
                    "",
                    new[] { "Mueva la cabeza despacio.", "El panel se acomoda solo.", "Cuando haya mirado a ambos lados, seguimos." },
                    true, 14f, 0.6f,
                    "Este es un simulador médico de cirugía endoscópica por trigonocefalia. Todavía no pulse botones."),

                S("intro.hands", "Mostrame tus manos",
                    "Levantá ambas manos frente a vos. Tenés que verlas.",
                    TutorialTargetKind.None, TutorialCompleteWhen.BothHandsVisible,
                    "",
                    new[] { "Manos a la vista de las cámaras.", "Si no aparecen, acercá las palmas.", "Con controladores: levantá los dos." },
                    true, 20f, 0.6f,
                    "Sin manos visibles el simulador no puede enseñarte. Mantenelas delante del visor."),

                S("intro.grab", "Agarrá y soltá",
                    "Acercá la mano al instrumento resaltado y hacé pellizco (índice + pulgar). Mantené.",
                    TutorialTargetKind.AnyGrabbable, TutorialCompleteWhen.GrabbedAny,
                    "AGARRÁ ACÁ",
                    new[] { "Pinch = agarrar (manos).", "Con controladores: Grip lateral.", "Si abrís los dedos, suelta." },
                    false, 0f, 0.8f,
                    "La herramienta del paso se resalta con un contorno brillante. Solo usá esa."),

                S("intro.trigger", "Cómo usar",
                    "Con la herramienta en la mano, mantenga el pellizco (o Trigger) para activarla.",
                    TutorialTargetKind.AnyGrabbable, TutorialCompleteWhen.TriggerWhileHoldingTarget,
                    "USE / PINCH",
                    new[] { "Pinch sostenido = usar.", "Con controladores: Trigger.", "Un toque basta en este paso." }),

                S("intro.colors", "Colores de ayuda",
                    "Verde = acción correcta o alineado. Amarillo = mal alineado, corrija la rotación.",
                    TutorialTargetKind.None, TutorialCompleteWhen.TimeoutOnly,
                    "",
                    new[] { "En el Kerrison: verde = puede morder.", "Amarillo = rote la herramienta.", "El contorno verde marca la herramienta del paso." },
                    true, 10f, 0.7f,
                    "Recuerde: verde bien · amarillo corregir."),

                S("intro.secondary", "Soltar / retirar",
                    "Abra el pellizco para soltar. Con controladores también B/Y.",
                    TutorialTargetKind.None, TutorialCompleteWhen.PressedSecondary,
                    "",
                    new[] { "Abrir dedos = soltar.", "B/Y en controladores suelta o retira.", "No camine con el stick." },
                    true, 16f, 0.7f)
            }
        };
    }

    static TutorialModuleConfig Interaction()
    {
        return new TutorialModuleConfig
        {
            id = "interact",
            title = "Interacción",
            description = "Tomar, mover, soltar y mirar una zona.",
            steps = new[]
            {
                S("interact.take", "Tomar un instrumento",
                    "Tome el instrumento resaltado. Grip y mantenga.",
                    TutorialTargetKind.Scalpel, TutorialCompleteWhen.GrabbedTarget,
                    "TÓMELO",
                    new[] { "Siga la flecha hasta la mesa.", "La herramienta parpadea.", "Grip lateral, no el índice." }),

                S("interact.move", "Movelo hasta acá",
                    "Observá la demo y después llevá la herramienta por la línea.",
                    TutorialTargetKind.Scalpel, TutorialCompleteWhen.GuidedMotionComplete,
                    "SEGUÍ LA LÍNEA",
                    new[] { "Primero mirá la mano fantasma.", "Tomá el bisturí y seguí el trazo.", "Si te desviás, te corrijo sin apuro." },
                    false, 0f, 1.0f,
                    "La demo enseña el movimiento. No corta sola: lo hacés vos."),

                S("interact.release", "Soltarlo",
                    "Suelte el Grip y deje el instrumento.",
                    TutorialTargetKind.Scalpel, TutorialCompleteWhen.ReleasedTarget,
                    "SUÉLTELO",
                    new[] { "Abra la mano (suelte Grip).", "Puede dejarlo en la mesa.", "Si se cae, tómelo de nuevo." }),

                S("interact.zone", "Mirar el campo",
                    "Mire la cabeza del paciente. Ahí se opera.",
                    TutorialTargetKind.PatientField, TutorialCompleteWhen.LookedAtTarget,
                    "CAMPO QUIRÚRGICO",
                    new[] { "Gire hacia el paciente.", "La flecha indica el campo.", "Ahí irá cada instrumento." },
                    true, 12f)
            }
        };
    }

    static TutorialModuleConfig Instruments()
    {
        return new TutorialModuleConfig
        {
            id = "tools",
            title = "Instrumental",
            description = "Reconocer cada herramienta de la mesa.",
            steps = new[]
            {
                Id("tools.marker", "Marcador", "Marcador / fibron. Sirve para dibujar la línea de incisión.", TutorialTargetKind.Marker, "MARCADOR"),
                Id("tools.scalpel", "Bisturí", "Bisturí. Corta la piel punto por punto.", TutorialTargetKind.Scalpel, "BISTURÍ"),
                Id("tools.retractor", "Retractor", "Retractor. Abre la herida para ver adentro.", TutorialTargetKind.Retractor, "RETRACTOR"),
                Id("tools.drill", "Taladro", "Taladro craneal. Perfora el hueso con control.", TutorialTargetKind.Drill, "TALADRO"),
                Id("tools.endo", "Endoscopio", "Endoscopio. La imagen sale en el monitor.", TutorialTargetKind.Endoscope, "ENDOSCOPIO"),
                Id("tools.kerrison", "Kerrison", "Kerrison. Muerde y extrae fragmentos de hueso.", TutorialTargetKind.Kerrison, "KERRISON"),
                Id("tools.coag", "Coagulador", "Coagulador. Trata el hueso y el sangrado.", TutorialTargetKind.Coagulator, "COAGULADOR"),
                Id("tools.hemo", "Hemostático", "Apósito hemostático. Se coloca sobre el lecho cruento.", TutorialTargetKind.Hemostatic, "HEMOSTÁTICO")
            }
        };
    }

    static TutorialModuleConfig Anatomy()
    {
        return new TutorialModuleConfig
        {
            id = "anatomy",
            title = "Zonas",
            description = "Dónde se trabaja en este procedimiento.",
            steps = new[]
            {
                S("anatomy.field", "Campo",
                    "Mire la frente. Ahí está la sutura metópica.",
                    TutorialTargetKind.PatientField, TutorialCompleteWhen.LookedAtTarget,
                    "FRENTE",
                    new[] { "Siga la flecha hacia el paciente.", "Esa zona es el campo de trabajo." },
                    true, 10f),

                S("anatomy.incision", "Línea de incisión",
                    "Mire los hitos de incisión. Ahí cortará con el bisturí.",
                    TutorialTargetKind.ScalpelNextPoint, TutorialCompleteWhen.LookedAtTarget,
                    "INCISIÓN",
                    new[] { "Son puntos en orden: 1, luego 2, luego 3.", "Ahora solo mírelos." },
                    true, 10f),

                S("anatomy.retractor", "Puntos de sujeción",
                    "Mire el punto de sujeción del retractor.",
                    TutorialTargetKind.RetractorSnap, TutorialCompleteWhen.LookedAtTarget,
                    "SUJECIÓN",
                    new[] { "Ahí se ancla la valva.", "No se deja el retractor en cualquier lado." },
                    true, 10f),

                S("anatomy.drill", "Punto de taladro",
                    "Mire el punto de anclaje del taladro.",
                    TutorialTargetKind.DrillSnap, TutorialCompleteWhen.LookedAtTarget,
                    "DRILL",
                    new[] { "La punta debe coincidir con ese punto.", "Luego se perfora con el Trigger." },
                    true, 10f),

                S("anatomy.access", "Acceso del endoscopio",
                    "Mire el acceso por donde entra el endoscopio.",
                    TutorialTargetKind.EndoscopeUnlock, TutorialCompleteWhen.LookedAtTarget,
                    "ACCESO",
                    new[] { "No se introduce por cualquier lado.", "La imagen se ve en el monitor, no en todo el visor." },
                    true, 10f)
            }
        };
    }

    static TutorialModuleConfig Procedure()
    {
        return new TutorialModuleConfig
        {
            id = "procedure",
            title = "Procedimiento",
            description = "Secuencia clínica guiada, con validación real.",
            steps = new[]
            {
                S("proc.mark", "Demarcación",
                    "Tome el marcador. Trace la línea de incisión sobre la piel.",
                    TutorialTargetKind.Marker, TutorialCompleteWhen.MarkerPainted,
                    "DIBUJE AQUÍ",
                    new[] { "Active el dibujo (Trigger o el evento de la herramienta).", "Una línea nítida basta.", "Si no pinta, acerque la punta a la piel." }),

                S("proc.cut", "Incisión",
                    "Tome el bisturí. Lleve la hoja al punto resaltado.",
                    TutorialTargetKind.ScalpelNextPoint, TutorialCompleteWhen.IncisionComplete,
                    "CORTE AQUÍ",
                    new[] { "Punto 1, luego 2, luego 3.", "Toque el hito con la hoja.", "Repita hasta el último hito." }),

                S("proc.retract", "Anclar retractor",
                    "Tome el retractor. Llévelo al punto de sujeción hasta que se ancle.",
                    TutorialTargetKind.RetractorSnap, TutorialCompleteWhen.RetractorAttached,
                    "ANCLE AQUÍ",
                    new[] { "La valva entra en el punto, no en el aire.", "Cuando se ancla, queda fijo.", "B/Y o el selector suelta si se equivoca." }),

                S("proc.open", "Abrir la herida",
                    "Con el retractor anclado, confirme la apertura del campo.",
                    TutorialTargetKind.RetractorSnap, TutorialCompleteWhen.RetractorOpened,
                    "CAMPO ABIERTO",
                    new[] { "Debe verse el acceso interno.", "Si no abrió, reanclé el retractor.", "No retire la valva todavía." }),

                S("proc.subcut", "Disección subcutánea",
                    "Siga el recorrido bajo la piel con Trigger mantenido.",
                    TutorialTargetKind.DissectionHalo, TutorialCompleteWhen.DissectionOnFontanelle,
                    "SIGA EL RECORRIDO",
                    new[] { "Halo rojo = se salió. Vuelva al camino.", "Movimientos cortos.", "No perfore." }),

                S("proc.fontanelle", "Fontanela",
                    "Continúe más lento sobre la fontanela. Trigger mantenido.",
                    TutorialTargetKind.DissectionHalo, TutorialCompleteWhen.DissectionComplete,
                    "FONTANELA",
                    new[] { "Más lento que el subcutáneo.", "Trabaje en superficie.", "Cuando termine, deje la herramienta." }),

                S("proc.drill", "Craniectomía",
                    "Use el taladro en la zona ideal. Active la perforación y mantenga.",
                    TutorialTargetKind.DrillSnap, TutorialCompleteWhen.DrillSucceeded,
                    "PERFORE AQUÍ",
                    new[] { "Busque la zona ideal (indicador verde).", "Active el taladro mientras está en contacto.", "Si sale, vuelva a la zona." }),

                S("proc.endo.in", "Introducir endoscopio",
                    "Active el endoscopio en el acceso. Trigger para avanzar.",
                    TutorialTargetKind.EndoscopeUnlock, TutorialCompleteWhen.EndoscopeDepthLow,
                    "ENTRE POR AQUÍ",
                    new[] { "Mire el monitor.", "Trigger = entra. B/Y = sale.", "Entre despacio." }),

                S("proc.endo.view", "Orientar la óptica",
                    "Avance hasta ver el campo interno con claridad.",
                    TutorialTargetKind.EndoscopeScreen, TutorialCompleteWhen.EndoscopeDepthWork,
                    "MONITOR",
                    new[] { "Llegue al menos a un tercio de profundidad.", "Si se pierde, retire un poco y vuelva.", "La imagen queda en el monitor." }),

                S("proc.kerrison", "Bocado Kerrison",
                    "Rote las mandíbulas hacia el hueso y mantenga Trigger.",
                    TutorialTargetKind.Kerrison, TutorialCompleteWhen.KerrisonHolding,
                    "KERRISON",
                    new[] { "Rote primero, Trigger después.", "Si no muerde, la rotación está mal.", "Grip firme." }),

                S("proc.deposit", "Depositar el hueso",
                    "Lleve el fragmento a la zona de depósito. El hueso no desaparece.",
                    TutorialTargetKind.ClearCol, TutorialCompleteWhen.KerrisonDeposited,
                    "DEPOSITE AQUÍ",
                    new[] { "El fragmento se suelta en esa zona.", "Si se cae, recójaco.", "Debe quedar a la vista." }),

                S("proc.coag", "Coagulación ósea",
                    "Mantenga el coagulador sobre la zona marcada hasta completar.",
                    TutorialTargetKind.CoagPath, TutorialCompleteWhen.CoagulationDone,
                    "COAGULE AQUÍ",
                    new[] { "Punta en contacto con la zona.", "Mantenga hasta el cambio visual.", "No salte al aire." }),

                S("proc.hemo", "Hemostasia",
                    "Coloque el apósito hemostático o complete el tratamiento del lecho.",
                    TutorialTargetKind.Hemostatic, TutorialCompleteWhen.HemostasisComplete,
                    "HEMOSTASIA",
                    new[] { "Pegue sobre tejido, no en el aire.", "Mantenga la presión el tiempo indicado.", "También cuenta el coagulador si ya terminó." }),

                S("proc.suture", "Suturectomía",
                    "Toque los tres puntos de sutura en orden: 1, 2 y 3.",
                    TutorialTargetKind.SuturePoint, TutorialCompleteWhen.SutureComplete,
                    "SUTURA",
                    new[] { "En orden. No salte el del medio.", "El hilo une los puntos.", "Toque el hito con la punta." }),

                S("proc.plasty", "Cierre cutáneo",
                    "Aproxime los bordes de piel. Trigger mantenido sobre el recorrido.",
                    TutorialTargetKind.PlastyPath, TutorialCompleteWhen.PlastyComplete,
                    "CIERRE",
                    new[] { "Mire los dos bordes acercarse.", "Mantenga Trigger.", "Si no hay herramienta de plástica, este paso se omite." })
            }
        };
    }

    static TutorialModuleConfig Finish()
    {
        return new TutorialModuleConfig
        {
            id = "finish",
            title = "Finalización",
            description = "Cerrar, repetir y practicar libre.",
            steps = new[]
            {
                S("finish.recap", "Procedimiento terminado",
                    "Procedimiento guiado completo. Puede revisar el campo o reiniciar.",
                    TutorialTargetKind.PatientField, TutorialCompleteWhen.TimeoutOnly,
                    "CAMPO",
                    new[] { "Mire el resultado con calma.", "Para repetir: REINICIAR ESCENA o F10.", "F3 reinicia el paso actual durante el tutorial." },
                    true, 8f, 0.7f,
                    "Se muestra el cartel de procedimiento terminado."),

                S("finish.free", "Modo libre",
                    "A partir de ahora practica sin guía constante. Menú del control pausa o reanuda.",
                    TutorialTargetKind.None, TutorialCompleteWhen.EnterFreeMode,
                    "",
                    new[] { "Puede repetir gestos por su cuenta.", "Reinicie la escena para un entrenamiento nuevo." },
                    true, 6f, 0.4f)
            }
        };
    }

    static TutorialStepConfig Id(string id, string title, string instruction, TutorialTargetKind target, string marker)
    {
        return S(id, title, instruction, target, TutorialCompleteWhen.LookedOrGrabbedTarget, marker,
            new[] { "Mírelo un momento o tómelo.", "La flecha indica cuál es.", "Luego lo usará en el procedimiento." },
            true, 10f, 0.45f);
    }

    static TutorialStepConfig S(
        string id, string title, string instruction,
        TutorialTargetKind target, TutorialCompleteWhen when,
        string marker, string[] hints,
        bool timeout = false, float timeoutSec = 0f, float delay = 0.8f,
        string detail = null)
    {
        return new TutorialStepConfig
        {
            id = id,
            title = title,
            instruction = instruction,
            detail = detail ?? string.Empty,
            hints = hints,
            target = target,
            markerLabel = marker,
            completeWhen = when,
            allowTimeout = timeout,
            timeoutSeconds = timeoutSec,
            delayBeforeNext = delay,
            skipIfTargetMissing = true
        };
    }
}
