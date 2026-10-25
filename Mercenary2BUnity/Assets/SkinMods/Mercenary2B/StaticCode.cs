using System;
using System.Collections.Generic;
using RoR2;
using UnityEngine;

namespace Mercenary2B
{

    public static class Helpers
    {
        private static GameObject prefabHolder;

        public static GameObject ClonePrefab(GameObject gameObject)
        {
            if (!prefabHolder)
            {
                prefabHolder = new GameObject("PrefabHolder");
                GameObject.DontDestroyOnLoad(prefabHolder);
                prefabHolder.SetActive(false);
            }

            var copy = GameObject.Instantiate(gameObject, prefabHolder.transform);
            copy.AddComponent<RemoveClone>();
            copy.name = gameObject.name;
            return copy;
        }

        public static Transform GetChild(this Transform transform, int index, out Transform child)
        {
            return child = transform.GetChild(index);
        }

        public static GameObject WithComponent<T>(this GameObject gameObject, Action<T> modifier = null) where T : Component
        {
            var component = gameObject.AddComponent<T>(); 
            modifier?.Invoke(component);
            return gameObject;
        }

        public static GameObject WithTryComponent<T>(this GameObject gameObject, Action<T> modifier = null) where T : Component
        {
            if (gameObject && !gameObject.GetComponent<T>())
            {
                var component = gameObject.AddComponent<T>();
                modifier?.Invoke(component);
            }

            return gameObject;
        }

        public static T ThrowIfEquals<T>(int index, string message, T value, T expected) where T : Enum
        {
            if (value.Equals(expected))
            {
                throw new ElementException(index, message);
            }

            return value;
        }

        public static T ThrowIfOutOfBounds<T>(int index, string message, T[] array, int elementIndex) where T : class
        {
            if (array is null || array.Length <= elementIndex)
            {
                throw new ElementException(index, message);
            }

            return array[elementIndex];
        }

        public static T ThrowIfNull<T>(int index, string message, T value) where T : class
        {
            if (value is null)
            {
                throw new ElementException(index, message);
            }

            return value;
        }

        public static void TryCatchThrow(string message, Action action)
        {
            try
            {
                action?.Invoke();
            }
            catch (Exception e)
            {
                throw new FieldException(message, e);
            }
        }
    }

    public class RemoveClone : MonoBehaviour
    {
        public void Awake()
        {
            name = name[..^7];
        }
    }

    public class FieldException : Exception
    {
        public FieldException(string message, Exception innerException) : base(message, innerException) { }
    }

    public class ElementException : Exception
    {
        public int Index { get; }
        public ElementException(int index, string message) : base(message)
        {
            Index = index;
        }
    }
    public class InsertBones : MonoBehaviour
    {
        private readonly List<SkinnedMeshRenderer> modifiedRenderers = new List<SkinnedMeshRenderer>();
        public List<string> rendererPaths = new List<string>();
        public List<Transform> newBones = new List<Transform>();
        public string afterPath;

        public void Awake()
        {
            var mdl = GetComponentInParent<ModelSkinController>().transform;
            var afterTransform = mdl.transform.Find(afterPath);
            foreach (var rendererPath in rendererPaths)
            {
                var renderer = mdl.Find(rendererPath)?.GetComponent<SkinnedMeshRenderer>();
                if (renderer)
                {
                    var bones = renderer.bones;
                    var index = Array.IndexOf(bones, afterTransform) + 1;
                    if (index != 0)
                    {
                        Array.Resize(ref bones, bones.Length + newBones.Count);
                        Array.Copy(bones, index, bones, index + newBones.Count, bones.Length - newBones.Count - index);
                        newBones.CopyTo(bones, index);
                        renderer.bones = bones;
                        modifiedRenderers.Add(renderer);
                    }
                }
            }
        }

        public void OnDestroy()
        {
            foreach (var renderer in modifiedRenderers)
            {
                if (renderer)
                {
                    var bones = renderer.bones;
                    var index = Array.IndexOf(bones, transform);
                    Array.Copy(bones, index + newBones.Count, bones, index, bones.Length - index - newBones.Count);
                    Array.Resize(ref bones, bones.Length - newBones.Count);
                    renderer.bones = bones;
                }
            }
            modifiedRenderers.Clear();
        }
    }

    public class AddDynamicColliders : MonoBehaviour
    {
        public List<string> paths = new List<string>();

        public void Awake()
        {
            var mdl = GetComponentInParent<ModelSkinController>().transform;
            var allColliders = new List<DynamicBoneCollider>();
            foreach (var path in paths)
            {
                var obj = mdl.Find(path);
                if (obj)
                {
                    allColliders.AddRange(obj.GetComponents<DynamicBoneCollider>());
                }
            }

            foreach (var dynamicSkin in GetComponents<DynamicBone>())
            {
                dynamicSkin.m_Colliders.AddRange(allColliders);
            }
        }
    }
}