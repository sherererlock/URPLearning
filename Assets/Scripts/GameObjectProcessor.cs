using UnityEngine;
using UnityEditor;

public class GameObjectProcessor : MonoBehaviour
{
    public Material targetMaterial;
    public MonoBehaviour scriptToAdd;

    void Start()
    {
        ProcessGameObjects();
    }

    [ContextMenu("ProcessGameObjects")]
    void ProcessGameObjects()
    {
        GameObject[] allGameObjects = GameObject.FindObjectsOfType<GameObject>();

        foreach (GameObject obj in allGameObjects)
        {
            // �Ƴ������ڽű�
            //RemoveMissingScriptsFromGameObject(obj);

            // �����MeshRenderer�����������Material
            MeshRenderer meshRenderer = obj.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                meshRenderer.material = targetMaterial;
                // ���ָ���Ľű�
                if (scriptToAdd != null)
                {
                    obj.AddComponent(scriptToAdd.GetType());
                }
            }
        }
    }

    private static void RemoveMissingScriptsFromGameObject(GameObject gameObject)
    {
        // ��ȡ���еĽű�
        MonoBehaviour[] scripts = gameObject.GetComponents<MonoBehaviour>();

        // ���ڼ�¼��Ҫ�Ƴ��Ľű�
        SerializedObject serializedObject = new SerializedObject(gameObject);
        SerializedProperty serializedProperty = serializedObject.FindProperty("m_Component");

        int scriptCount = 0;

        for (int i = 0; i < scripts.Length; i++)
        {
            if (scripts[i] == null)
            {
                // �Ƴ�Missing Script
                serializedProperty.DeleteArrayElementAtIndex(i - scriptCount);
                scriptCount++;
            }
        }

        // Ӧ���޸�
        serializedObject.ApplyModifiedProperties();
    }
}
