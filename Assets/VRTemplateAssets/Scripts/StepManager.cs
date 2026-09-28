using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Unity.VRTemplate
{
    /// <summary>
    /// Controls the steps in the in coaching card.
    /// </summary>
    public class StepManager : MonoBehaviour
    {
        [Serializable]
        class Step
        {
            [SerializeField]
            public GameObject stepObject;

            [SerializeField]
            public string buttonText;
        }

        [SerializeField]
        public TextMeshProUGUI m_StepButtonTextField;

        [SerializeField]
        List<Step> m_StepList = new List<Step>();

        int m_CurrentStepIndex = 0;

        public void Next()
        {
            if (ProcedureManager.Instance != null)
            {
                ProcedureManager.Instance.RequestContinueFromBoard();
                return;
            }

            m_StepList[m_CurrentStepIndex].stepObject.SetActive(false);
            m_CurrentStepIndex = (m_CurrentStepIndex + 1) % m_StepList.Count;
            m_StepList[m_CurrentStepIndex].stepObject.SetActive(true);
            m_StepButtonTextField.text = m_StepList[m_CurrentStepIndex].buttonText;
        }

        public void ShowProcedure(string title, string instruction)
        {
            if (m_StepList.Count == 0)
                return;

            for (int i = 0; i < m_StepList.Count; i++)
            {
                if (m_StepList[i].stepObject != null)
                    m_StepList[i].stepObject.SetActive(i == 0);
            }

            m_CurrentStepIndex = 0;
            GameObject card = m_StepList[0].stepObject;
            if (card == null)
                return;

            TMP_Text body = null;
            TMP_Text[] texts = card.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i].gameObject.name.IndexOf("Modal", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    body = texts[i];
                    break;
                }
            }

            if (body == null)
                return;

            string heading = string.IsNullOrWhiteSpace(title) ? "" : title.Trim();
            string detail = string.IsNullOrWhiteSpace(instruction) ? "" : instruction.Trim();
            if (heading.Length == 0)
                body.text = detail;
            else if (detail.Length == 0)
                body.text = heading;
            else
                body.text = heading + "\n\n" + detail;

            if (m_StepButtonTextField != null)
            {
                Button button = m_StepButtonTextField.GetComponentInParent<Button>();
                if (button != null)
                {
                    bool allow = ProcedureManager.Instance == null || ProcedureManager.Instance.BoardCanContinue();
                    button.gameObject.SetActive(allow);
                }
            }
        }
    }
}
