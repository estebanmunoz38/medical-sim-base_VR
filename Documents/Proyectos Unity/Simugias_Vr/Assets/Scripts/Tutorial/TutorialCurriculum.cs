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

                S("intro.hands", "Sus manos",
                    "Levante ambas manos frente a usted. Debe verlas.",
                    TutorialTargetKind.None, TutorialCompleteWhen.ControllersMoved,
                    "",
                    new[] { "Mueva las manos delante de su cara.", "Si no las ve, no tape las cámaras del visor.", "Todo se hace con las manos, no con mouse." },
                    true, 12f, 0.6f),

                S("intro.grab", "Cómo agarrar",
                    "Acerque la mano a un instrumento y apriete el Grip (lateral). Manténgalo.",
                    TutorialTargetKind.AnyGrabbable, TutorialCompleteWhen.GrabbedAny,
                    "AGARRE AQUÍ",
                    new[] { "Grip = dedo medio contra el mango.", "El Trigger (índice) no agarra.", "Mantenga el Grip. Si lo suelta, el objeto cae." }),

                S("intro.trigger", "Cómo usar",
                    "Con el instrumento en la mano, pulse el Trigger (gatillo del índice).",
                    TutorialTargetKind.AnyGrabbable, TutorialCompleteWhen.TriggerWhileHoldingTarget,
                    "PULSE TRIGGER",
                    new[] { "Primero Grip para no soltarlo.", "Trigger = gatillo de adelante.", "Un toque basta en este paso." }),

                S("intro.secondary", "Botón B / Y",
                    "Pulse una vez el botón B (derecho) o Y (izquierdo).",
                    TutorialTargetKind.None, TutorialCompleteWhen.PressedSecondary,
                    "",
                    new[] { "B está en la cara del control derecho.", "Luego servirá para soltar o retirar.", "No camine con el stick." },
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
                    TutorialTargetKind.Shave, TutorialCompleteWhen.GrabbedTarget,
                    "TÓMELO",
                    new[] { "Siga la flecha hasta la mesa.", "La herramienta parpadea.", "Grip lateral, no el índice." }),

                S("interact.move", "Moverlo",
                    "Muévalo un poco en el aire, sin soltarlo.",
                    TutorialTargetKind.Shave, TutorialCompleteWhen.MovedHeldTarget,
                    "MUÉVALO",
                    new[] { "Mantenga el Grip.", "Un movimiento corto basta.", "Todavía no lo use en el paciente." }),

                S("interact.release", "Soltarlo",
                    "Suelte el Grip y deje el instrumento.",
                    TutorialTargetKind.Shave, TutorialCompleteWhen.ReleasedTarget,
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
                Id("tools.shave", "Rasurado", "Máquina de rasurado. Deja la piel al descubierto.", TutorialTargetKind.Shave, "RASURADOR"),
                Id("tools.marker", "Marcador", "Marcador. Sirve para dibujar la línea de incisión.", TutorialTargetKind.Marker, "MARCADOR"),
                Id("tools.scalpel", "Bisturí", "Bisturí. Corta la piel punto por punto.", TutorialTargetKind.Scalpel, "BISTURÍ"),
                Id("tools.retractor", "Retractor", "Retractor. Abre la herida para ver adentro.", TutorialTargetKind.Retractor, "RETRACTOR"),
                Id("tools.drill", "Taladro", "Taladro craneal. Perfora el hueso con control.", TutorialTargetKind.Drill, "TALADRO"),
                Id("tools.endo", "Endoscopio", "Endoscopio. La imagen sale en el monitor.", TutorialTargetKind.Endoscope, "ENDOSCOPIO"),
                Id("tools.kerrison", "Kerrison", "Kerrison. Muerde y extrae fragmentos de hueso.", TutorialTargetKind.Kerrison, "KERRISON"),
                Id("tools.coag", "Coagulador", "Coagulador. Trata el hueso y el sangrado.", TutorialTargetKind.Coagulator, "COAGULADOR")
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
                S("proc.shave", "Rasurado",
                    "Tome el rasurador. Punta contra la piel y Trigger mantenido.",
                    TutorialTargetKind.Shave, TutorialCompleteWhen.ShaveComplete,
                    "RASURE AQUÍ",
                    new[] { "Pase en franjas sobre la frente.", "Si no rasura, acerque más la punta.", "Cubra el campo hasta que se complete." },
                    false, 0f, 0.9f,
                    "Antes de pintar hay que rasurar."),

                S("proc.mark", "Demarcación",
                    "Tome el marcador. Trace la línea de incisión sobre la piel rasurada.",
                    TutorialTargetKind.Marker, TutorialCompleteWhen.MarkerPainted,
                    "DIBUJE AQUÍ",
                    new[] { "Trigger mantenido = pinta.", "Si no pinta, termine el rasurado.", "Una línea nítida basta." }),

                S("proc.cut", "Incisión",
                    "Tome el bisturí. Lleve la hoja al punto y pulse Trigger una vez.",
                    TutorialTargetKind.ScalpelNextPoint, TutorialCompleteWhen.IncisionComplete,
                    "CORTE AQUÍ",
                    new[] { "Punto, clic, siguiente punto.", "Si pulsa lejos, no cuenta.", "Repita hasta el último hito." }),

                S("proc.retract", "Anclar retractor",
                    "Tome el retractor. Llévelo al punto de sujeción y pulse Trigger.",
                    TutorialTargetKind.RetractorSnap, TutorialCompleteWhen.RetractorAttached,
                    "ANCLE AQUÍ",
                    new[] { "La valva entra en el punto, no en el aire.", "Trigger ancla.", "B/Y suelta si se equivoca." }),

                S("proc.open", "Abrir la herida",
                    "Con el retractor anclado, suba la mano para abrir.",
                    TutorialTargetKind.RetractorSnap, TutorialCompleteWhen.RetractorOpened,
                    "ELEVE",
                    new[] { "Movimiento real hacia arriba.", "Debe verse la piel interna.", "Si no abre, confirme que está anclado." }),

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
                    "Ancle el taladro en el punto. Mantenga Trigger para perforar.",
                    TutorialTargetKind.DrillSnap, TutorialCompleteWhen.DrillSucceeded,
                    "PERFORE AQUÍ",
                    new[] { "Punta en el snap, Trigger para anclar.", "Mantenga para profundizar.", "B/Y retira si quedó trabado." }),

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
                    "Siga el recorrido de coagulación con Trigger mantenido.",
                    TutorialTargetKind.CoagPath, TutorialCompleteWhen.CoagulationDone,
                    "COAGULE AQUÍ",
                    new[] { "Punta al inicio del recorrido.", "No salte al aire.", "Coagulación primero." }),

                S("proc.hemo", "Hemostasia",
                    "Trate el lecho cruento. Trigger sobre el recorrido o coloque el apósito.",
                    TutorialTargetKind.CoagPath, TutorialCompleteWhen.HemostasisComplete,
                    "HEMOSTASIA",
                    new[] { "Pegue sobre tejido, no en el aire.", "Complete el recorrido.", "También puede usar el apósito hemostático." }),

                S("proc.suture", "Suturectomía",
                    "Toque los tres puntos de sutura en orden: 1, 2 y 3.",
                    TutorialTargetKind.SuturePoint, TutorialCompleteWhen.SutureComplete,
                    "SUTURA",
                    new[] { "En orden. No salte el del medio.", "El hilo une los puntos.", "Toque el hito con la punta." }),

                S("proc.plasty", "Cierre cutáneo",
                    "Aproxime los bordes de piel. Trigger mantenido sobre el recorrido.",
                    TutorialTargetKind.PlastyPath, TutorialCompleteWhen.PlastyComplete,
                    "CIERRE",
                    new[] { "Mire los dos bordes acercarse.", "Mantenga Trigger.", "Este es el último gesto clínico." })
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
                S("finish.recap", "Procedimiento guiado",
                    "Puede dejar los instrumentos. Grip agarra. Trigger usa. B/Y suelta o retira.",
                    TutorialTargetKind.PatientField, TutorialCompleteWhen.TimeoutOnly,
                    "CAMPO",
                    new[] { "Mire el resultado con calma.", "Para repetir todo: reinicie la escena.", "El modo libre quita las flechas." },
                    true, 8f, 0.7f),

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
