using UnityEngine;
using TMPro;
using Floex.Physiology;

/// <summary>
/// Drives the patient monitor screen (on the ECG/hospital monitor) from PatientState.
/// Reads the sim state one-way and writes the vital readouts — DISPLAY ONLY.
/// No thresholds, no alarm colouring, no judging values (that's clinical/KRB, later).
///
/// Attach to the Patient_Monitor screen root (under the ECG_Monitor canvas).
/// Assign the PatientStateDriver (the PatientSim object). Reads driver.State each refresh.
///
/// Until physiology/scenario data drives PatientState, this shows its resting
/// placeholder values sitting static — expected.
/// </summary>
public class PatientMonitorController : MonoBehaviour
{
    [Header("Source")]
    [Tooltip("The PatientStateDriver (PatientSim). If left empty, found at runtime.")]
    public PatientStateDriver driver;

    [Header("Refresh")]
    [Tooltip("Seconds between display refreshes. Values change slowly; no need for every frame.")]
    public float refreshInterval = 0.25f;

    float _timer;

    void OnEnable()
    {
        if (driver == null) driver = FindObjectOfType<PatientStateDriver>();
        Refresh();
    }

    void Update()
    {
        _timer += Time.deltaTime;
        if (_timer < refreshInterval) return;
        _timer = 0f;
        Refresh();
    }

    void Refresh()
    {
        if (driver == null || driver.State == null) return;
        PatientState s = driver.State;

        SetText("Txt_HR_Value",    Mathf.RoundToInt((float)s.HeartRate).ToString());
        SetText("Txt_BP_Value",    Mathf.RoundToInt((float)s.BloodPressure).ToString());
        SetText("Txt_SvO2_Value",  Mathf.RoundToInt((float)s.SvO2).ToString());
        SetText("Txt_Temp_Value",  s.Temperature.ToString("0.0"));
        SetText("Txt_PaO2_Value",  Mathf.RoundToInt((float)s.ArterialPO2).ToString());
        SetText("Txt_PaCO2_Value", Mathf.RoundToInt((float)s.ArterialPCO2).ToString());

        SetText("Txt_BE_Value",     s.BaseExcess.ToString("0.0") + " mEq/L");
        SetText("Txt_Hct_Value",    Mathf.RoundToInt((float)s.Hematocrit).ToString() + " %");
        SetText("Txt_Bypass_Value", s.TimeOnBypassClock());
    }

    void SetText(string name, string value)
    {
        Transform t = FindDeep(transform, name);
        if (t == null) return;
        TMP_Text tmp = t.GetComponent<TMP_Text>();
        if (tmp != null) tmp.text = value;
    }

    static Transform FindDeep(Transform root, string name)
    {
        if (root.name == name) return root;
        foreach (Transform child in root)
        {
            Transform found = FindDeep(child, name);
            if (found != null) return found;
        }
        return null;
    }
}