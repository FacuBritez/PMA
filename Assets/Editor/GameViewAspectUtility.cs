using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class GameViewAspectUtility
{
    static GameViewAspectUtility()
    {
        EditorApplication.delayCall += Ensure16_3AspectExists;
    }

    [MenuItem("Tools/Aspect Ratio/Añadir 16:3 a GameView")]
    public static void Ensure16_3AspectExists()
    {
        try
        {
            Type sizesType = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSizes");
            if (sizesType == null) return;

            PropertyInfo instanceProp = sizesType.GetProperty("instance", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (instanceProp == null) return;

            object sizesInstance = instanceProp.GetValue(null);
            if (sizesInstance == null) return;

            PropertyInfo currentGroupProp = sizesType.GetProperty("currentGroup", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (currentGroupProp == null) return;

            object currentGroup = currentGroupProp.GetValue(sizesInstance);
            if (currentGroup == null) return;

            Type groupType = currentGroup.GetType();
            MethodInfo getBuiltinCount = groupType.GetMethod("GetBuiltinCount");
            MethodInfo getCustomCount = groupType.GetMethod("GetCustomCount");
            MethodInfo getGameViewSize = groupType.GetMethod("GetGameViewSize");
            MethodInfo addCustomSize = groupType.GetMethod("AddCustomSize");

            int totalCustom = (int)getCustomCount.Invoke(currentGroup, null);
            for (int i = 0; i < totalCustom; i++)
            {
                object sizeObj = getGameViewSize.Invoke(currentGroup, new object[] { i });
                PropertyInfo textProp = sizeObj.GetType().GetProperty("baseText");
                if (textProp != null)
                {
                    string name = (string)textProp.GetValue(sizeObj);
                    if (name != null && name.Contains("16:3"))
                    {
                        return; // Ya existe
                    }
                }
            }

            Type sizeType = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSize");
            Type sizeTypeEnum = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSizeType");
            // GameViewSizeType.AspectRatio = 0
            object aspectEnumVal = Enum.ToObject(sizeTypeEnum, 0);

            ConstructorInfo ctor = sizeType.GetConstructor(new Type[] { sizeTypeEnum, typeof(int), typeof(int), typeof(string) });
            if (ctor != null)
            {
                object newSize = ctor.Invoke(new object[] { aspectEnumVal, 16, 3, "16:3 Ultra-Wide" });
                addCustomSize.Invoke(currentGroup, new object[] { newSize });
                Debug.Log("[GameViewAspectUtility] Resolución 16:3 añadida al GameView.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[GameViewAspectUtility] No se pudo registrar automáticamente la resolución 16:3: " + ex.Message);
        }
    }
}
