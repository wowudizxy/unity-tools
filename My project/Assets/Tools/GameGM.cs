using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;



#if UNITY_EDITOR
using UnityEditor;
#endif

public class GameGM : MonoBehaviour
{
    private static GameGM _instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreateGM()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (_instance != null)
            return;

        GameGM exist = FindObjectOfType<GameGM>();
        if (exist != null)
        {
            _instance = exist;
            return;
        }

        GameObject go = new GameObject("[GM]");
        _instance = go.AddComponent<GameGM>();
        DontDestroyOnLoad(go);

        Debug.Log("GM 自动创建成功");
#endif
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ==========================
    // 下面写你的 GM 方法
    // ==========================

    [GMButton("打印")]
    private void OpenRankRewardWindow(string text)
    {
        Debug.Log(text);
    }
    
}

[AttributeUsage(AttributeTargets.Method)]
public class GMButtonAttribute : Attribute
{
    public string ButtonName;
    public int Order;

    public GMButtonAttribute(string buttonName, int order = 0)
    {
        ButtonName = buttonName;
        Order = order;
    }
}

#if UNITY_EDITOR

[CustomEditor(typeof(GameGM))]
public class GameGMEditor : Editor
{
    private static readonly Dictionary<string, object> ParamValueDict =
        new Dictionary<string, object>();

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GUILayout.Space(10);
        GUILayout.Label("GM 测试工具", EditorStyles.boldLabel);

        GameGM gm = (GameGM)target;

        List<MethodInfo> gmMethods = GetGMMethods();

        foreach (MethodInfo method in gmMethods)
        {
            DrawGMMethod(gm, method);
        }
    }

    private List<MethodInfo> GetGMMethods()
    {
        MethodInfo[] methods = typeof(GameGM).GetMethods(
            BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.NonPublic
        );

        List<MethodInfo> result = new List<MethodInfo>();

        foreach (MethodInfo method in methods)
        {
            GMButtonAttribute attr = method.GetCustomAttribute<GMButtonAttribute>();
            if (attr != null)
            {
                result.Add(method);
            }
        }

        result.Sort((a, b) =>
        {
            GMButtonAttribute attrA = a.GetCustomAttribute<GMButtonAttribute>();
            GMButtonAttribute attrB = b.GetCustomAttribute<GMButtonAttribute>();
            return attrA.Order.CompareTo(attrB.Order);
        });

        return result;
    }

    private void DrawGMMethod(GameGM gm, MethodInfo method)
    {
        GMButtonAttribute attr = method.GetCustomAttribute<GMButtonAttribute>();
        ParameterInfo[] parameters = method.GetParameters();

        GUILayout.Space(8);

        EditorGUILayout.BeginVertical("box");

        EditorGUILayout.LabelField(attr.ButtonName, EditorStyles.boldLabel);

        object[] invokeParams = new object[parameters.Length];
        bool canInvoke = true;

        for (int i = 0; i < parameters.Length; i++)
        {
            ParameterInfo param = parameters[i];

            string key = GetParamKey(method, i);

            object value = GetParamValue(key, param);

            bool support;
            value = DrawParameterField(param.Name, param.ParameterType, value, out support);

            if (!support)
            {
                canInvoke = false;
            }

            ParamValueDict[key] = value;
            invokeParams[i] = value;
        }

        GUI.enabled = canInvoke;

        if (GUILayout.Button("执行", GUILayout.Height(28)))
        {
            try
            {
                method.Invoke(gm, invokeParams);
            }
            catch (Exception e)
            {
                Debug.LogError($"GM 执行失败：{method.Name}\n{e}");
            }
        }

        GUI.enabled = true;

        EditorGUILayout.EndVertical();
    }

    private string GetParamKey(MethodInfo method, int index)
    {
        return $"{method.Name}_{index}";
    }

    private object GetParamValue(string key, ParameterInfo param)
    {
        if (ParamValueDict.TryGetValue(key, out object value))
        {
            return value;
        }

        if (param.HasDefaultValue)
        {
            return param.DefaultValue;
        }

        Type type = param.ParameterType;

        if (type == typeof(int))
            return 0;

        if (type == typeof(long))
            return 0L;

        if (type == typeof(float))
            return 0f;

        if (type == typeof(double))
            return 0d;

        if (type == typeof(string))
            return "";

        if (type == typeof(bool))
            return false;

        if (type == typeof(Vector2))
            return Vector2.zero;

        if (type == typeof(Vector3))
            return Vector3.zero;

        if (type.IsEnum)
        {
            Array values = Enum.GetValues(type);
            if (values.Length > 0)
                return values.GetValue(0);
        }

        return null;
    }

    private object DrawParameterField(
        string label,
        Type type,
        object value,
        out bool support)
    {
        support = true;

        if (type == typeof(int))
            return EditorGUILayout.IntField(label, Convert.ToInt32(value));

        if (type == typeof(long))
            return EditorGUILayout.LongField(label, Convert.ToInt64(value));

        if (type == typeof(float))
            return EditorGUILayout.FloatField(label, Convert.ToSingle(value));

        if (type == typeof(double))
            return EditorGUILayout.DoubleField(label, Convert.ToDouble(value));

        if (type == typeof(string))
            return EditorGUILayout.TextField(label, value == null ? "" : value.ToString());

        if (type == typeof(bool))
            return EditorGUILayout.Toggle(label, Convert.ToBoolean(value));

        if (type == typeof(Vector2))
            return EditorGUILayout.Vector2Field(label, (Vector2)value);

        if (type == typeof(Vector3))
            return EditorGUILayout.Vector3Field(label, (Vector3)value);

        if (type.IsEnum)
            return EditorGUILayout.EnumPopup(label, (Enum)value);

        support = false;

        EditorGUILayout.HelpBox(
            $"参数 {label} 的类型 {type.Name} 暂不支持自动生成输入框",
            MessageType.Warning
        );

        return value;
    }
}

#endif