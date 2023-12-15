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
            // 移除不存在脚本
            //RemoveMissingScriptsFromGameObject(obj);

            // 如果有MeshRenderer组件，设置其Material
            MeshRenderer meshRenderer = obj.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                meshRenderer.material = targetMaterial;
                // 添加指定的脚本
                if (scriptToAdd != null)
                {
                    obj.AddComponent(scriptToAdd.GetType());
                }
            }
        }
    }

    private static void RemoveMissingScriptsFromGameObject(GameObject gameObject)
    {
        // 获取所有的脚本
        MonoBehaviour[] scripts = gameObject.GetComponents<MonoBehaviour>();

        // 用于记录需要移除的脚本
        SerializedObject serializedObject = new SerializedObject(gameObject);
        SerializedProperty serializedProperty = serializedObject.FindProperty("m_Component");

        int scriptCount = 0;

        for (int i = 0; i < scripts.Length; i++)
        {
            if (scripts[i] == null)
            {
                // 移除Missing Script
                serializedProperty.DeleteArrayElementAtIndex(i - scriptCount);
                scriptCount++;
            }
        }

        // 应用修改
        serializedObject.ApplyModifiedProperties();
    }
}
