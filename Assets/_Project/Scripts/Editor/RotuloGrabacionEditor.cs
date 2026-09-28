using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RotuloGrabacion))]
public class RotuloGrabacionEditor : Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUILayout.HelpBox(
            "En Play queda en una esquina de lo que se ve con los anteojos y sigue a la cabeza. No usa la vista plana de la pantalla, que el casco no muestra.",
            MessageType.Info);

        DrawDefaultInspector();

        RotuloGrabacion rotulo = (RotuloGrabacion)target;
        EditorGUILayout.Space(8f);

        if (Application.isPlaying)
            EditorGUILayout.LabelField("Tiempo", rotulo.TiempoVisible);

        using (new EditorGUI.DisabledScope(!Application.isPlaying))
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Reiniciar contador"))
                rotulo.ReiniciarContador();

            if (GUILayout.Button(rotulo.Contando ? "Pausar" : "Continuar"))
                rotulo.AlternarContador();
            EditorGUILayout.EndHorizontal();
        }

        if (!Application.isPlaying)
            EditorGUILayout.LabelField("El contador corre al entrar en Play.", EditorStyles.miniLabel);

        if (Application.isPlaying && !EditorGUIUtility.editingTextField)
            Repaint();
    }
}
