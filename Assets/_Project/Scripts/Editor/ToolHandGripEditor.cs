using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ToolHandGrip))]
public class ToolHandGripEditor : Editor
{
    public override void OnInspectorGUI()
    {
        ToolHandGrip grip = (ToolHandGrip)target;
        EditorGUILayout.HelpBox(
            "Caja amarilla: zona donde hay que pellizcar.\n" +
            "Esfera blanca: punto que entra en la mano.\n" +
            "Flecha cian: hacia dónde apunta la punta.\n" +
            "Tarjeta: palma aproximada. Verde = dedos, rojo = pulgar, azul = dorso.\n" +
            "En la escena, la esfera mueve el punto de agarre y la tarjeta mueve el desplazamiento de la palma.",
            MessageType.Info);

        DrawDefaultInspector();

        if (grip.tipAxis.sqrMagnitude < 0.000001f)
            EditorGUILayout.HelpBox("El eje de punta está en cero. La orientación del agarre no queda definida.", MessageType.Warning);
        if (grip.separateLeftHand && grip.leftPalmOffset.sqrMagnitude < 0.0000001f && grip.palmOffset.sqrMagnitude > 0.0000001f)
            EditorGUILayout.HelpBox("La mano izquierda tiene desplazamiento en cero. Va a sentarse en el centro de la palma.", MessageType.Warning);
        float largestZone = Mathf.Max(grip.grabHalfExtents.x, Mathf.Max(grip.grabHalfExtents.y, grip.grabHalfExtents.z));
        if (largestZone > 0.08f)
            EditorGUILayout.HelpBox("La zona de agarre supera 8 cm. Puede incluir la punta.", MessageType.Warning);

        if (grip.TryGetFunctionalTip(out Vector3 functionalTip))
        {
            Vector3 axis = grip.transform.rotation * (grip.tipAxis.sqrMagnitude < 0.000001f ? Vector3.forward : grip.tipAxis.normalized);
            Vector3 towardTip = functionalTip - grip.GripWorld(grip.transform.position, grip.transform.rotation);
            if (towardTip.sqrMagnitude > 0.0004f)
            {
                float angle = Vector3.Angle(axis, towardTip);
                if (angle > 35f)
                {
                    EditorGUILayout.HelpBox(
                        "La punta funcional (magenta) no coincide con el eje de punta (cian). Diferencia " + angle.ToString("0") + "°.",
                        MessageType.Warning);
                }
            }
        }

        if (Application.isPlaying && grip.debugPose)
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Estado", grip.Phase.ToString());
            EditorGUILayout.LabelField("Mano", string.IsNullOrEmpty(grip.OwnerHand) ? "—" : grip.OwnerHand);
            EditorGUILayout.LabelField("Tracking", grip.TrackingValid ? "válido" : "no válido");
            EditorGUILayout.LabelField("Gracia", grip.GraceRemaining.ToString("0.00") + " s");
            EditorGUILayout.LabelField("Error posición", (grip.PositionErrorMeters * 1000f).ToString("0.0") + " mm");
            EditorGUILayout.LabelField("Error ángulo", grip.AngleErrorDegrees.ToString("0.0") + "°");
            EditorGUILayout.LabelField("Temblor quieto", grip.QuietPositionMm.ToString("0.0") + " mm / " + grip.QuietAngleDegrees.ToString("0.0") + "°");
            EditorGUILayout.LabelField("Pérdidas de tracking", grip.TrackingLosses.ToString());
            EditorGUILayout.LabelField("Oclusiones de dedos", grip.FingerOcclusions.ToString());
            EditorGUILayout.LabelField("Muestras descartadas", grip.OutliersRejected.ToString());
            EditorGUILayout.LabelField("Soltados no pedidos", grip.UnexpectedReleases.ToString());
            EditorGUILayout.LabelField("Recuperaciones", grip.TrackingRecoveries.ToString());
            EditorGUILayout.LabelField("Reacquisiciones", grip.Reacquisitions.ToString());
            EditorGUILayout.LabelField("Hueco a la superficie", grip.ContactGapMm.ToString("0.0") + " mm");
            EditorGUILayout.LabelField("Corrección de contacto", grip.ContactCorrectionMm.ToString("0.0") + " mm");
            Repaint();
        }

        if (Application.isPlaying && grip.recordSession)
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.HelpBox(GripSessionLog.Summary(), MessageType.None);
            if (GUILayout.Button("Guardar resumen ahora"))
                GripSessionLog.Flush();
            if (!string.IsNullOrEmpty(GripSessionLog.LastPath))
                EditorGUILayout.LabelField("Archivo", GripSessionLog.LastPath);
        }

        EditorGUILayout.Space(6f);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Preset fibrón"))
            Apply(grip, ToolGripPreset.Marker);
        if (GUILayout.Button("Preset rasuradora"))
            Apply(grip, ToolGripPreset.Shaver);
        EditorGUILayout.EndHorizontal();
    }

    static void Apply(ToolHandGrip grip, ToolGripPreset preset)
    {
        Undo.RecordObject(grip, "Aplicar preset de agarre");
        grip.Apply(preset);
        EditorUtility.SetDirty(grip);
    }

    void OnSceneGUI()
    {
        ToolHandGrip grip = (ToolHandGrip)target;
        if (grip == null)
            return;

        Transform tool = grip.transform;
        Vector3 gripWorld = grip.GripWorld(tool.position, tool.rotation);
        EditorGUI.BeginChangeCheck();
        Vector3 movedGrip = Handles.PositionHandle(gripWorld, tool.rotation);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(grip, "Mover punto de agarre");
            grip.gripPositionMeters += Quaternion.Inverse(tool.rotation) * (movedGrip - gripWorld);
            EditorUtility.SetDirty(grip);
            gripWorld = grip.GripWorld(tool.position, tool.rotation);
        }

        bool left = grip.previewLeftHand;
        grip.PreviewPalm(left, out Vector3 palmPos, out Quaternion palmRot);
        Handles.color = new Color(0.7f, 0.85f, 1f, 0.9f);
        EditorGUI.BeginChangeCheck();
        Vector3 movedPalm = Handles.PositionHandle(palmPos, palmRot);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(grip, "Mover palma");
            Quaternion align = ToolHandGrip.MapRotation(
                grip.tipAxis.sqrMagnitude < 0.000001f ? Vector3.forward : grip.tipAxis.normalized,
                grip.upAxis.sqrMagnitude < 0.000001f ? Vector3.up : grip.upAxis.normalized,
                grip.tipInHand.sqrMagnitude < 0.000001f ? Vector3.forward : grip.tipInHand.normalized,
                grip.upInHand.sqrMagnitude < 0.000001f ? Vector3.up : grip.upInHand.normalized);
            Quaternion adjust = tool.rotation * Quaternion.Inverse(align);
            Vector3 offset = Quaternion.Inverse(adjust) * (gripWorld - movedPalm);
            if (left && grip.separateLeftHand)
                grip.leftPalmOffset = offset;
            else
                grip.palmOffset = offset;
            EditorUtility.SetDirty(grip);
        }

        Handles.Label(gripWorld + Vector3.up * 0.02f, "Agarre");
        Handles.Label(palmPos + (palmRot * Vector3.up) * 0.09f, left ? "Palma izquierda" : "Palma");
    }
}
