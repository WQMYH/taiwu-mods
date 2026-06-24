using System;
using System.Reflection;

namespace CopyBuildingModernized.Frontend
{
    public static class ReflectionExtensions
    {
        public static T GetFieldValue<T>(this object instance, string fieldName)
        {
            if (instance == null || string.IsNullOrEmpty(fieldName))
                return default;

            FieldInfo field = instance.GetType().GetField(fieldName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (field == null)
                return default;

            object value = field.GetValue(instance);
            if (value == null)
                return default;

            return value is T typed ? typed : default;
        }

        public static bool TrySetPrivateField(this object instance, string fieldName, object value)
        {
            if (instance == null || string.IsNullOrEmpty(fieldName))
                return false;

            FieldInfo field = instance.GetType().GetField(fieldName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (field == null)
                return false;

            field.SetValue(instance, value);
            return true;
        }

        public static void SetPrivateField(this object instance, string fieldName, object value)
        {
            TrySetPrivateField(instance, fieldName, value);
        }

        public static T CallPrivateMethod<T>(this object instance, string methodName, params object[] parameters)
        {
            if (instance == null || string.IsNullOrEmpty(methodName))
                return default;

            MethodInfo method = instance.GetType().GetMethod(methodName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (method == null)
                return default;

            object value = method.Invoke(instance, parameters);
            if (value == null)
                return default;

            return value is T typed ? typed : default;
        }
    }
}
