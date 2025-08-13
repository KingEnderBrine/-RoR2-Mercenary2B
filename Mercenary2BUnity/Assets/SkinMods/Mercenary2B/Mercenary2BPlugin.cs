using BepInEx;
using BepInEx.Logging;
using RoR2;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using UnityEngine;
using System.Security.Permissions;
using MonoMod.RuntimeDetour.HookGen;
using RoR2.ContentManagement;
using UnityEngine.AddressableAssets;
using RoR2.Projectile;


#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618 // Type or member is obsolete
namespace Mercenary2B
{
    
    [BepInPlugin("com.KingEnderBrine.Mercenary2B","Mercenary2B","1.3.2")]
    public partial class Mercenary2BPlugin : BaseUnityPlugin
    {
        internal static Mercenary2BPlugin Instance { get; private set; }
        internal static ManualLogSource InstanceLogger => Instance?.Logger;
        
        private static AssetBundle assetBundle;
        private static readonly List<Material> materialsWithRoRShader = new List<Material>();
        private void Start()
        {
            Instance = this;

            BeforeStart();

            using (var assetStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Mercenary2B.kingenderbrinemercenary2b"))
            {
                assetBundle = AssetBundle.LoadFromStream(assetStream);
            }

            BodyCatalog.availability.CallWhenAvailable(BodyCatalogInit);
            HookEndpointManager.Add(typeof(Language).GetMethod(nameof(Language.LoadStrings)), (Action<Action<Language>, Language>)LanguageLoadStrings);

            ReplaceShaders();

            AfterStart();
        }

        partial void BeforeStart();
        partial void AfterStart();
        static partial void BeforeBodyCatalogInit();
        static partial void AfterBodyCatalogInit();

        private static void ReplaceShaders()
        {
            LoadMaterialsWithReplacedShader(@"RoR2/Base/Shaders/HGStandard.shader"
                ,@"Assets/Mercenary2B/Resources/Materials/mat2B.mat"                ,@"Assets/Mercenary2B/Resources/Materials/matSword.mat");
        }

        private static void LoadMaterialsWithReplacedShader(string shaderPath, params string[] materialPaths)
        {
            var shader = Addressables.LoadAssetAsync<Shader>(shaderPath).WaitForCompletion();
            foreach (var materialPath in materialPaths)
            {
                var material = assetBundle.LoadAsset<Material>(materialPath);
                material.shader = shader;
                materialsWithRoRShader.Add(material);
            }
        }

        private static void LanguageLoadStrings(Action<Language> orig, Language self)
        {
            orig(self);

            self.SetStringByToken("KINGENDERBRINE_SKIN_MERCENARY2B_NAME", "2B");
        }

        private static void BodyCatalogInit()
        {
            BeforeBodyCatalogInit();

            AddMercBodyMercenary2BSkin();

            AfterBodyCatalogInit();
        }

        static partial void MercBodyMercenary2BSkinAdded(SkinDef skinDef, GameObject bodyPrefab);

        private static void AddMercBodyMercenary2BSkin()
        {
            var bodyName = "MercBody";
            var skinName = "Mercenary2B";
            try
            {
                var bodyPrefab = BodyCatalog.FindBodyPrefab(bodyName);
                if (!bodyPrefab)
                {
                    InstanceLogger.LogWarning($"Failed to add \"{skinName}\" skin because \"{bodyName}\" doesn't exist");
                    return;
                }

                var modelLocator = bodyPrefab.GetComponent<ModelLocator>();
                if (!modelLocator)
                {
                    InstanceLogger.LogWarning($"Failed to add \"{skinName}\" skin to \"{bodyName}\" because it doesn't have \"ModelLocator\" component");
                    return;
                }

                var mdl = modelLocator.modelTransform.gameObject;
                var skinController = mdl ? mdl.GetComponent<ModelSkinController>() : null;
                if (!skinController)
                {
                    InstanceLogger.LogWarning($"Failed to add \"{skinName}\" skin to \"{bodyName}\" because it doesn't have \"ModelSkinController\" component");
                    return;
                }

                var renderers = mdl.GetComponentsInChildren<Renderer>(true);
                var lights = mdl.GetComponentsInChildren<Light>(true);

                var skin = ScriptableObject.CreateInstance<SkinDef>();
                var skinParams = ScriptableObject.CreateInstance<SkinDefParams>();
                skin.skinDefParams = skinParams;

                TryCatchThrow("Icon", () =>
                {
                    skin.icon = assetBundle.LoadAsset<Sprite>(@"Assets\SkinMods\Mercenary2B\Icons\Mercenary2BIcon.png");
                });
                skin.name = skinName;
                skin.nameToken = "KINGENDERBRINE_SKIN_MERCENARY2B_NAME";
                skin.rootObject = mdl;
                TryCatchThrow("Base Skins", () =>
                {
                    skin.baseSkins = new SkinDef[] 
                    { 
                        skinController.skins[0],
                    };
                });
                TryCatchThrow("Unlockable Name", () =>
                {
                    skin.unlockableDef = null;
                });
                TryCatchThrow("Game Object Activations", () =>
                {
                    skinParams.gameObjectActivations = Array.Empty<SkinDefParams.GameObjectActivation>();
                });
                TryCatchThrow("Renderer Infos", () =>
                {
                    skinParams.rendererInfos = new CharacterModel.RendererInfo[]
                    {
                        new CharacterModel.RendererInfo
                        {
                            defaultMaterial = assetBundle.LoadAsset<Material>(@"Assets/Mercenary2B/Resources/Materials/mat2B.mat"),
                            defaultShadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On,
                            ignoreOverlays = false,
                            renderer = renderers.First(r => r.name == "MercMesh")
                        },
                        new CharacterModel.RendererInfo
                        {
                            defaultMaterial = assetBundle.LoadAsset<Material>(@"Assets/Mercenary2B/Resources/Materials/matSword.mat"),
                            defaultShadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On,
                            ignoreOverlays = false,
                            renderer = renderers.First(r => r.name == "MercSwordMesh")
                        },
                    };
                });
                TryCatchThrow("Mesh Replacements", () =>
                {
                    skinParams.meshReplacements = new SkinDefParams.MeshReplacement[]
                    {
                        new SkinDefParams.MeshReplacement
                        {
                            mesh = assetBundle.LoadAsset<Mesh>(@"Assets\SkinMods\Mercenary2B\Meshes\Nier2b.mesh"),
                            renderer = renderers.First(r => r.name == "MercMesh")
                        },
                        new SkinDefParams.MeshReplacement
                        {
                            mesh = assetBundle.LoadAsset<Mesh>(@"Assets\SkinMods\Mercenary2B\Meshes\Sword.mesh"),
                            renderer = renderers.First(r => r.name == "MercSwordMesh")
                        },
                    };
                });
                TryCatchThrow("Light Infos", () =>
                {
                    skinParams.lightReplacements = new CharacterModel.LightInfo[]
                    {
                    };
                });
                TryCatchThrow("Minion Skin Replacements", () =>
                {
                    skinParams.minionSkinReplacements = Array.Empty<SkinDefParams.MinionSkinReplacement>();
                });
                TryCatchThrow("Projectile Ghost Replacements", () =>
                {
                    skinParams.projectileGhostReplacements = Array.Empty<SkinDefParams.ProjectileGhostReplacement>();
                });

                Array.Resize(ref skinController.skins, skinController.skins.Length + 1);
                skinController.skins[skinController.skins.Length - 1] = skin;

                MercBodyMercenary2BSkinAdded(skin, bodyPrefab);
            }
            catch (FieldException e)
            {
                InstanceLogger.LogWarning($"Failed to add \"{skinName}\" skin to \"{bodyName}\"");
                InstanceLogger.LogWarning($"Field causing issue: {e.Message}");
                InstanceLogger.LogError(e.InnerException);
            }
            catch (Exception e)
            {
                InstanceLogger.LogWarning($"Failed to add \"{skinName}\" skin to \"{bodyName}\"");
                InstanceLogger.LogError(e);
            }
        }

        private static void TryCatchThrow(string message, Action action)
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
        
        private static void TryAddComponent<T>(GameObject obj) where T : Component
        {
            if (!obj.GetComponent<T>())
            {
                obj.AddComponent<T>();
            }
        }

        private class FieldException : Exception
        {
            public FieldException(string message, Exception innerException) : base(message, innerException) { }
        }
    }
}