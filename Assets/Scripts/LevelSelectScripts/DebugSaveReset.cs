using UnityEngine;

public class DebugSaveReset : MonoBehaviour
{
    [SerializeField] private int totalLevels = 10;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Delete))
        {
            SaveManager.ResetSave(totalLevels);
            Debug.Log("Save reset.");
        }
    }
}