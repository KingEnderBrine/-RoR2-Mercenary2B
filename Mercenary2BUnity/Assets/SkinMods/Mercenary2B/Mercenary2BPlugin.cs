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
using BepInEx.Configuration;
using static Mercenary2B.Helpers;

#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618 // Type or member is obsolete
namespace Mercenary2B
{
    [BepInPlugin("com.KingEnderBrine.Mercenary2B", "Mercenary2B", "1.3.4")]
    public partial class Mercenary2BPlugin : BaseUnityPlugin
    {
        internal static Mercenary2BPlugin Instance { get; private set; }
        internal static ManualLogSource InstanceLogger => Instance?.Logger;
        
        private static AssetBundle assetBundle;
        private static readonly List<Material> materialsWithRoRShader = new List<Material>();
        public static ConfigEntry<bool> Mercenary2BEnableSkirtConfig;
        
        private void Start()
        {
            Instance = this;
            
            BeforeStart();
            
            Config.SaveOnConfigSet = false;
            Mercenary2BEnableSkirtConfig = Config.Bind("Mercenary2B", "EnableSkirt", true);
            Config.SaveOnConfigSet = true;
            Config.Save();
            
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
            LoadMaterialsWithReplacedShader(
                @"RoR2/Base/Shaders/HGStandard.shader"
                ,@"Assets/Mercenary2B/Resources/Materials/mat2B.mat"
                ,@"Assets/Mercenary2B/Resources/Materials/matSword.mat");
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
        
        private static SkinDef CreateMercBodyMercenary2BSkin(GameObject mdl, ModelSkinController skinController)
        {
            var renderers = mdl.GetComponentsInChildren<Renderer>(true);
            var lights = mdl.GetComponentsInChildren<Light>(true);
            var skinnedRendererPaths = new List<string>();
            foreach (var renderer in renderers)
            {
                if (renderer is SkinnedMeshRenderer)
                {
                    skinnedRendererPaths.Add(Util.BuildPrefabTransformPath(mdl.transform, renderer.transform, false, false));
                }
            }
            
            var skin = ScriptableObject.CreateInstance<SkinDef>();
            var skinParams = ScriptableObject.CreateInstance<SkinDefParams>();
            skin.skinDefParams = skinParams;
            skin.name = "Mercenary2B";
            skin.nameToken = "KINGENDERBRINE_SKIN_MERCENARY2B_NAME";
            skin.rootObject = mdl;
            TryCatchThrow("Icon", () =>
            {
                skin.icon = assetBundle.LoadAsset<Sprite>(@"Assets\SkinMods\Mercenary2B\Icons\Mercenary2BIcon.png");
            });
            TryCatchThrow("Unlockable Name", () =>
            {
                skin.unlockableDef = null;
            });
            TryCatchThrow("Base Skins", () =>
            {
                skin.baseSkins = new SkinDef[] 
                {
                    ThrowIfOutOfBounds(0, "Index 0 is out of bounds of skins array", skinController.skins, 0),
                };
            });
            TryCatchThrow("Game Object Activations", () =>
            {
                var list = new List<SkinDefParams.GameObjectActivation>();
                list.Add(new SkinDefParams.GameObjectActivation
                {
                    gameObject = ThrowIfNull(0, "There is no renderer at path \"MercArmature/ROOT/base/stomach/chest/SwingCenter/SwordBase/Point Light\"", mdl.transform.Find("MercArmature/ROOT/base/stomach/chest/SwingCenter/SwordBase/Point Light")).gameObject,
                    shouldActivate = false,
                    spawnPrefabOnModelObject = false,
                });
                list.Add(new SkinDefParams.GameObjectActivation
                {
                    gameObject = ThrowIfNull(1, "There is no renderer at path \"MercArmature/ROOT/base/stomach/chest/Point Light\"", mdl.transform.Find("MercArmature/ROOT/base/stomach/chest/Point Light")).gameObject,
                    shouldActivate = false,
                    spawnPrefabOnModelObject = false,
                });
                list.Add(new SkinDefParams.GameObjectActivation
                {
                    gameObject = ThrowIfNull(2, "There is no renderer at path \"MercArmature/ROOT/base/stomach/chest/SwingCenter/SwordBase\"", mdl.transform.Find("MercArmature/ROOT/base/stomach/chest/SwingCenter/SwordBase")).gameObject,
                    shouldActivate = false,
                    spawnPrefabOnModelObject = true,
                    localPosition = new Vector3(0f, 0f, 0f),
                    localRotation = new Vector3(0f, 0f, 0f),
                    localScale = new Vector3(1f, 1f, 1f),
                    prefab = ClonePrefab(assetBundle.LoadAsset<GameObject>(@"Assets/Mercenary2B/Resources/Prefabs/SwordAccessoryPrefab.prefab"))
                });
                list.Add(new SkinDefParams.GameObjectActivation
                {
                    gameObject = ThrowIfNull(3, "There is no renderer at path \"MercArmature/ROOT/base/pelvis/thigh.l\"", mdl.transform.Find("MercArmature/ROOT/base/pelvis/thigh.l")).gameObject,
                    shouldActivate = false,
                    spawnPrefabOnModelObject = true,
                    localPosition = new Vector3(0f, 0f, 0f),
                    localRotation = new Vector3(0f, 0f, 0f),
                    localScale = new Vector3(1f, 1f, 1f),
                    prefab = ClonePrefab(assetBundle.LoadAsset<GameObject>(@"Assets/Mercenary2B/Resources/Prefabs/ThighLDBCollider.prefab"))
                });
                list.Add(new SkinDefParams.GameObjectActivation
                {
                    gameObject = ThrowIfNull(4, "There is no renderer at path \"MercArmature/ROOT/base/pelvis/thigh.r\"", mdl.transform.Find("MercArmature/ROOT/base/pelvis/thigh.r")).gameObject,
                    shouldActivate = false,
                    spawnPrefabOnModelObject = true,
                    localPosition = new Vector3(0f, 0f, 0f),
                    localRotation = new Vector3(0f, 0f, 0f),
                    localScale = new Vector3(1f, 1f, 1f),
                    prefab = ClonePrefab(assetBundle.LoadAsset<GameObject>(@"Assets/Mercenary2B/Resources/Prefabs/ThighRDBCollider.prefab"))
                });
                list.Add(new SkinDefParams.GameObjectActivation
                {
                    gameObject = ThrowIfNull(5, "There is no renderer at path \"MercArmature/ROOT/base/pelvis\"", mdl.transform.Find("MercArmature/ROOT/base/pelvis")).gameObject,
                    shouldActivate = false,
                    spawnPrefabOnModelObject = true,
                    localPosition = new Vector3(8.702916E-07f, 0.2023992f, -0.004584247f),
                    localRotation = new Vector3(78.44302f, 0.02800226f, 0.03399968f),
                    localScale = new Vector3(1f, 1f, 1f),
                    prefab = ClonePrefab(assetBundle.LoadAsset<GameObject>(@"Assets/Mercenary2B/Resources/Prefabs/butt_base.prefab"))
                        .WithComponent<InsertBones>(c => 
                        {
                            c.rendererPaths.AddRange(skinnedRendererPaths);
                            c.afterPath = @"MercArmature/ROOT/base/pelvis/thigh.r/calf.r/foot.r/toe.r";
                            var t = c.transform;
                            c.newBones.Add(t);
                            c.newBones.Add(t.GetChild(0, out var t_0));
                            c.newBones.Add(t.GetChild(1, out var t_1));
                        })
                });
                list.Add(new SkinDefParams.GameObjectActivation
                {
                    gameObject = ThrowIfNull(6, "There is no renderer at path \"MercArmature/ROOT/base/stomach/chest\"", mdl.transform.Find("MercArmature/ROOT/base/stomach/chest")).gameObject,
                    shouldActivate = false,
                    spawnPrefabOnModelObject = true,
                    localPosition = new Vector3(-0.0001998614f, 0.08633288f, 0.01216672f),
                    localRotation = new Vector3(81.93136f, 173.5348f, 173.8761f),
                    localScale = new Vector3(0.9999998f, 0.9999999f, 0.9999999f),
                    prefab = ClonePrefab(assetBundle.LoadAsset<GameObject>(@"Assets/Mercenary2B/Resources/Prefabs/breast_base.prefab"))
                        .WithComponent<InsertBones>(c => 
                        {
                            c.rendererPaths.AddRange(skinnedRendererPaths);
                            c.afterPath = @"MercArmature/ROOT/base/stomach/chest/SwingCenter/SwordBase/SwordHandPlacement.r";
                            var t = c.transform;
                            c.newBones.Add(t);
                            c.newBones.Add(t.GetChild(0, out var t_0));
                            c.newBones.Add(t.GetChild(1, out var t_1));
                        })
                });
                if (Mercenary2BEnableSkirtConfig.Value)
                {
                    list.Add(new SkinDefParams.GameObjectActivation
                    {
                        gameObject = ThrowIfNull(7, "There is no renderer at path \"MercArmature/ROOT/base/stomach\"", mdl.transform.Find("MercArmature/ROOT/base/stomach")).gameObject,
                        shouldActivate = false,
                        spawnPrefabOnModelObject = true,
                        localPosition = new Vector3(0f, 0f, 0f),
                        localRotation = new Vector3(-1.024528E-05f, -2.697236E-08f, 1.075947E-08f),
                        localScale = new Vector3(1f, 1f, 1f),
                        prefab = ClonePrefab(assetBundle.LoadAsset<GameObject>(@"Assets/Mercenary2B/Resources/Prefabs/SkirtPrefab.prefab"))
                            .WithComponent<AddDynamicColliders>(c => 
                            {
                                c.paths.Add(@"MercArmature/ROOT/base/pelvis/butt_base");
                                c.paths.Add(@"MercArmature/ROOT/base/pelvis/thigh.l/ThighLDBCollider");
                                c.paths.Add(@"MercArmature/ROOT/base/pelvis/thigh.r/ThighRDBCollider");
                            })
                    });
                }
                skinParams.gameObjectActivations = list.ToArray();
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
                        renderer = ThrowIfNull(0, "There is no renderer with the name \"MercMesh\"", renderers.FirstOrDefault(r => r.name == "MercMesh")),
                        hideOnDeath = false,
                        ignoresMaterialOverrides = false,
                    },
                    new CharacterModel.RendererInfo
                    {
                        defaultMaterial = assetBundle.LoadAsset<Material>(@"Assets/Mercenary2B/Resources/Materials/matSword.mat"),
                        defaultShadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On,
                        ignoreOverlays = false,
                        renderer = ThrowIfNull(1, "There is no renderer with the name \"MercSwordMesh\"", renderers.FirstOrDefault(r => r.name == "MercSwordMesh")),
                        hideOnDeath = false,
                        ignoresMaterialOverrides = false,
                    },
                };
            });
            TryCatchThrow("MeshReplacements", () =>
            {
                skinParams.meshReplacements = new SkinDefParams.MeshReplacement[] 
                {
                    new SkinDefParams.MeshReplacement
                    {
                        mesh = assetBundle.LoadAsset<Mesh>(@"Assets\SkinMods\Mercenary2B\Meshes\Nier2b.mesh"),
                        renderer = ThrowIfNull(0, "There is no renderer with the name \"MercMesh\"", renderers.FirstOrDefault(r => r.name == "MercMesh")),
                    },
                    new SkinDefParams.MeshReplacement
                    {
                        mesh = assetBundle.LoadAsset<Mesh>(@"Assets\SkinMods\Mercenary2B\Meshes\Sword.mesh"),
                        renderer = ThrowIfNull(1, "There is no renderer with the name \"MercSwordMesh\"", renderers.FirstOrDefault(r => r.name == "MercSwordMesh")),
                    },
                };
            });
            TryCatchThrow("Light Infos", () =>
            {
                skinParams.lightReplacements = Array.Empty<CharacterModel.LightInfo>();
            });
            TryCatchThrow("Minion Skin Replacements", () =>
            {
                skinParams.minionSkinReplacements = Array.Empty<SkinDefParams.MinionSkinReplacement>();
            });
            TryCatchThrow("Projectile Ghost Replacements", () =>
            {
                skinParams.projectileGhostReplacements = Array.Empty<SkinDefParams.ProjectileGhostReplacement>();
            });
            return skin;
        }
        
        static partial void MercBodyMercenary2BLobbySkinAdded(SkinDef skinDef, GameObject displayPrefab);
        
        private static SkinDef CreateMercBodyMercenary2BLobbySkin(GameObject mdl, ModelSkinController skinController)
        {
            var renderers = mdl.GetComponentsInChildren<Renderer>(true);
            var lights = mdl.GetComponentsInChildren<Light>(true);
            var skinnedRendererPaths = new List<string>();
            foreach (var renderer in renderers)
            {
                if (renderer is SkinnedMeshRenderer)
                {
                    skinnedRendererPaths.Add(Util.BuildPrefabTransformPath(mdl.transform, renderer.transform, false, false));
                }
            }
            
            var skin = ScriptableObject.CreateInstance<SkinDef>();
            var skinParams = ScriptableObject.CreateInstance<SkinDefParams>();
            skin.skinDefParams = skinParams;
            skin.name = "Mercenary2B";
            skin.nameToken = "KINGENDERBRINE_SKIN_MERCENARY2B_NAME";
            skin.rootObject = mdl;
            TryCatchThrow("Icon", () =>
            {
                skin.icon = assetBundle.LoadAsset<Sprite>(@"Assets\SkinMods\Mercenary2B\Icons\Mercenary2BIcon.png");
            });
            TryCatchThrow("Unlockable Name", () =>
            {
                skin.unlockableDef = null;
            });
            TryCatchThrow("Base Skins", () =>
            {
                skin.baseSkins = new SkinDef[] 
                {
                    ThrowIfOutOfBounds(0, "Index 0 is out of bounds of skins array", skinController.skins, 0),
                };
            });
            TryCatchThrow("Game Object Activations", () =>
            {
                var list = new List<SkinDefParams.GameObjectActivation>();
                list.Add(new SkinDefParams.GameObjectActivation
                {
                    gameObject = ThrowIfNull(0, "There is no renderer at path \"MercArmature/ROOT/base/stomach/chest/SwingCenter/SwordBase/Point Light\"", mdl.transform.Find("MercArmature/ROOT/base/stomach/chest/SwingCenter/SwordBase/Point Light")).gameObject,
                    shouldActivate = false,
                    spawnPrefabOnModelObject = false,
                });
                list.Add(new SkinDefParams.GameObjectActivation
                {
                    gameObject = ThrowIfNull(1, "There is no renderer at path \"MercArmature/ROOT/base/stomach/chest/Point Light (1)\"", mdl.transform.Find("MercArmature/ROOT/base/stomach/chest/Point Light (1)")).gameObject,
                    shouldActivate = false,
                    spawnPrefabOnModelObject = false,
                });
                list.Add(new SkinDefParams.GameObjectActivation
                {
                    gameObject = ThrowIfNull(2, "There is no renderer at path \"MercArmature/ROOT/base/stomach/chest/SwingCenter/SwordBase\"", mdl.transform.Find("MercArmature/ROOT/base/stomach/chest/SwingCenter/SwordBase")).gameObject,
                    shouldActivate = false,
                    spawnPrefabOnModelObject = true,
                    localPosition = new Vector3(0f, 0f, 0f),
                    localRotation = new Vector3(0f, 0f, 0f),
                    localScale = new Vector3(1f, 1f, 1f),
                    prefab = ClonePrefab(assetBundle.LoadAsset<GameObject>(@"Assets/Mercenary2B/Resources/Prefabs/SwordAccessoryPrefab.prefab"))
                });
                list.Add(new SkinDefParams.GameObjectActivation
                {
                    gameObject = ThrowIfNull(3, "There is no renderer at path \"MercArmature/ROOT/base/pelvis/thigh.l\"", mdl.transform.Find("MercArmature/ROOT/base/pelvis/thigh.l")).gameObject,
                    shouldActivate = false,
                    spawnPrefabOnModelObject = true,
                    localPosition = new Vector3(0f, 0f, 0f),
                    localRotation = new Vector3(0f, 0f, 0f),
                    localScale = new Vector3(1f, 1f, 1f),
                    prefab = ClonePrefab(assetBundle.LoadAsset<GameObject>(@"Assets/Mercenary2B/Resources/Prefabs/ThighLDBCollider.prefab"))
                });
                list.Add(new SkinDefParams.GameObjectActivation
                {
                    gameObject = ThrowIfNull(4, "There is no renderer at path \"MercArmature/ROOT/base/pelvis/thigh.r\"", mdl.transform.Find("MercArmature/ROOT/base/pelvis/thigh.r")).gameObject,
                    shouldActivate = false,
                    spawnPrefabOnModelObject = true,
                    localPosition = new Vector3(0f, 0f, 0f),
                    localRotation = new Vector3(0f, 0f, 0f),
                    localScale = new Vector3(1f, 1f, 1f),
                    prefab = ClonePrefab(assetBundle.LoadAsset<GameObject>(@"Assets/Mercenary2B/Resources/Prefabs/ThighRDBCollider.prefab"))
                });
                list.Add(new SkinDefParams.GameObjectActivation
                {
                    gameObject = ThrowIfNull(5, "There is no renderer at path \"MercArmature/ROOT/base/pelvis\"", mdl.transform.Find("MercArmature/ROOT/base/pelvis")).gameObject,
                    shouldActivate = false,
                    spawnPrefabOnModelObject = true,
                    localPosition = new Vector3(8.702916E-07f, 0.2023992f, -0.004584247f),
                    localRotation = new Vector3(78.44302f, 0.02800226f, 0.03399968f),
                    localScale = new Vector3(1f, 1f, 1f),
                    prefab = ClonePrefab(assetBundle.LoadAsset<GameObject>(@"Assets/Mercenary2B/Resources/Prefabs/butt_base.prefab"))
                        .WithComponent<InsertBones>(c => 
                        {
                            c.rendererPaths.AddRange(skinnedRendererPaths);
                            c.afterPath = @"MercArmature/ROOT/base/pelvis/thigh.r/calf.r/foot.r/toe.r";
                            var t = c.transform;
                            c.newBones.Add(t);
                            c.newBones.Add(t.GetChild(0, out var t_0));
                            c.newBones.Add(t.GetChild(1, out var t_1));
                        })
                });
                list.Add(new SkinDefParams.GameObjectActivation
                {
                    gameObject = ThrowIfNull(6, "There is no renderer at path \"MercArmature/ROOT/base/stomach/chest\"", mdl.transform.Find("MercArmature/ROOT/base/stomach/chest")).gameObject,
                    shouldActivate = false,
                    spawnPrefabOnModelObject = true,
                    localPosition = new Vector3(-0.0001998614f, 0.08633288f, 0.01216672f),
                    localRotation = new Vector3(81.93136f, 173.5348f, 173.8761f),
                    localScale = new Vector3(0.9999998f, 0.9999999f, 0.9999999f),
                    prefab = ClonePrefab(assetBundle.LoadAsset<GameObject>(@"Assets/Mercenary2B/Resources/Prefabs/breast_base.prefab"))
                        .WithComponent<InsertBones>(c => 
                        {
                            c.rendererPaths.AddRange(skinnedRendererPaths);
                            c.afterPath = @"MercArmature/ROOT/base/stomach/chest/SwingCenter/SwordBase/SwordHandPlacement.r";
                            var t = c.transform;
                            c.newBones.Add(t);
                            c.newBones.Add(t.GetChild(0, out var t_0));
                            c.newBones.Add(t.GetChild(1, out var t_1));
                        })
                });
                if (Mercenary2BEnableSkirtConfig.Value)
                {
                    list.Add(new SkinDefParams.GameObjectActivation
                    {
                        gameObject = ThrowIfNull(7, "There is no renderer at path \"MercArmature/ROOT/base/stomach\"", mdl.transform.Find("MercArmature/ROOT/base/stomach")).gameObject,
                        shouldActivate = false,
                        spawnPrefabOnModelObject = true,
                        localPosition = new Vector3(0f, 0f, 0f),
                        localRotation = new Vector3(-1.024528E-05f, -2.697236E-08f, 1.075947E-08f),
                        localScale = new Vector3(1f, 1f, 1f),
                        prefab = ClonePrefab(assetBundle.LoadAsset<GameObject>(@"Assets/Mercenary2B/Resources/Prefabs/SkirtPrefab.prefab"))
                            .WithComponent<AddDynamicColliders>(c => 
                            {
                                c.paths.Add(@"MercArmature/ROOT/base/pelvis/butt_base");
                                c.paths.Add(@"MercArmature/ROOT/base/pelvis/thigh.l/ThighLDBCollider");
                                c.paths.Add(@"MercArmature/ROOT/base/pelvis/thigh.r/ThighRDBCollider");
                            })
                    });
                }
                skinParams.gameObjectActivations = list.ToArray();
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
                        renderer = ThrowIfNull(0, "There is no renderer with the name \"MercMesh\"", renderers.FirstOrDefault(r => r.name == "MercMesh")),
                        hideOnDeath = false,
                        ignoresMaterialOverrides = false,
                    },
                    new CharacterModel.RendererInfo
                    {
                        defaultMaterial = assetBundle.LoadAsset<Material>(@"Assets/Mercenary2B/Resources/Materials/matSword.mat"),
                        defaultShadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On,
                        ignoreOverlays = false,
                        renderer = ThrowIfNull(1, "There is no renderer with the name \"MercSwordMesh\"", renderers.FirstOrDefault(r => r.name == "MercSwordMesh")),
                        hideOnDeath = false,
                        ignoresMaterialOverrides = false,
                    },
                };
            });
            TryCatchThrow("MeshReplacements", () =>
            {
                skinParams.meshReplacements = new SkinDefParams.MeshReplacement[] 
                {
                    new SkinDefParams.MeshReplacement
                    {
                        mesh = assetBundle.LoadAsset<Mesh>(@"Assets\SkinMods\Mercenary2B\Meshes\Nier2b.mesh"),
                        renderer = ThrowIfNull(0, "There is no renderer with the name \"MercMesh\"", renderers.FirstOrDefault(r => r.name == "MercMesh")),
                    },
                    new SkinDefParams.MeshReplacement
                    {
                        mesh = assetBundle.LoadAsset<Mesh>(@"Assets\SkinMods\Mercenary2B\Meshes\Sword.mesh"),
                        renderer = ThrowIfNull(1, "There is no renderer with the name \"MercSwordMesh\"", renderers.FirstOrDefault(r => r.name == "MercSwordMesh")),
                    },
                };
            });
            TryCatchThrow("Light Infos", () =>
            {
                skinParams.lightReplacements = Array.Empty<CharacterModel.LightInfo>();
            });
            TryCatchThrow("Minion Skin Replacements", () =>
            {
                skinParams.minionSkinReplacements = Array.Empty<SkinDefParams.MinionSkinReplacement>();
            });
            TryCatchThrow("Projectile Ghost Replacements", () =>
            {
                skinParams.projectileGhostReplacements = Array.Empty<SkinDefParams.ProjectileGhostReplacement>();
            });
            return skin;
        }
        
        private static void AddMercBodyMercenary2BSkin()
        {
            
            var bodyName = "MercBody";
            var skinName = "Mercenary2B";
            var bodyPrefab = BodyCatalog.FindBodyPrefab(bodyName);
            if (!bodyPrefab)
            {
                InstanceLogger.LogWarning($"Failed to add \"{skinName}\" skin because \"{bodyName}\" doesn't exist");
                return;
            }
            
            try
            {
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
                
                var skin = CreateMercBodyMercenary2BSkin(mdl, skinController);
                
                Array.Resize(ref skinController.skins, skinController.skins.Length + 1);
                skinController.skins[skinController.skins.Length - 1] = skin;
                
                MercBodyMercenary2BSkinAdded(skin, bodyPrefab);
            }
            catch (FieldException e)
            {
                if (e.InnerException is ElementException ie)
                {
                    InstanceLogger.LogWarning($"Failed to add \"{skinName}\" skin to \"{bodyName}\"");
                    InstanceLogger.LogWarning($"Field causing issue: {e.Message}, element: {ie.Index}");
                    InstanceLogger.LogWarning(ie.Message);
                    InstanceLogger.LogError(e.InnerException);
                }
                else
                {
                    InstanceLogger.LogWarning($"Failed to add \"{skinName}\" skin to \"{bodyName}\"");
                    InstanceLogger.LogWarning($"Field causing issue: {e.Message}");
                    InstanceLogger.LogError(e.InnerException);
                }
            }
            catch (Exception e)
            {
                InstanceLogger.LogWarning($"Failed to add \"{skinName}\" skin to \"{bodyName}\"");
                InstanceLogger.LogError(e);
            }
            
            try
            {
                var survivorDef = ContentManager.survivorDefs.FirstOrDefault(d => d.bodyPrefab == bodyPrefab);
                if (!survivorDef)
                {
                    InstanceLogger.LogWarning($"Failed to add \"{skinName}\" lobby skin to \"{bodyName}\" because SurvivorDef doesn't exist");
                    return;
                }
                
                var displayPrefab = survivorDef.displayPrefab;
                var displaySkinController = displayPrefab.GetComponentInChildren<ModelSkinController>();
                if (!displaySkinController)
                {
                    InstanceLogger.LogWarning($"Failed to add \"{skinName}\" lobby skin to \"{bodyName}\" because it doesn't have \"ModelSkinController\" component");
                    return;
                }
                
                var displayMdl = displaySkinController.gameObject;
                var displaySkin = CreateMercBodyMercenary2BLobbySkin(displayMdl, displaySkinController);
                
                Array.Resize(ref displaySkinController.skins, displaySkinController.skins.Length + 1);
                displaySkinController.skins[^1] = displaySkin;
                
                MercBodyMercenary2BLobbySkinAdded(displaySkin, displayPrefab);
            }
            catch (FieldException e)
            {
                if (e.InnerException is ElementException ie)
                {
                    InstanceLogger.LogWarning($"Failed to add \"{skinName}\" lobby skin to \"{bodyName}\"");
                    InstanceLogger.LogWarning($"Field causing issue: {e.Message}, element: {ie.Index}");
                    InstanceLogger.LogWarning(ie.Message);
                    InstanceLogger.LogError(e.InnerException);
                }
                else
                {
                    InstanceLogger.LogWarning($"Failed to add \"{skinName}\" lobby skin to \"{bodyName}\"");
                    InstanceLogger.LogWarning($"Field causing issue: {e.Message}");
                    InstanceLogger.LogError(e.InnerException);
                }
            }
            catch (Exception e)
            {
                InstanceLogger.LogWarning($"Failed to add \"{skinName}\" lobby skin to \"{bodyName}\"");
                InstanceLogger.LogError(e);
            }
        }
    }
}