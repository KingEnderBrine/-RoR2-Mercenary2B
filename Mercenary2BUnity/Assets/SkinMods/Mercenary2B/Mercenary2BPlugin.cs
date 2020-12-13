using BepInEx;
using R2API;
using R2API.Utils;
using RoR2;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using UnityEngine;

namespace Mercenary2B
{
    [R2APISubmoduleDependency(nameof(LoadoutAPI), nameof(LanguageAPI))]
    [NetworkCompatibility(CompatibilityLevel.NoNeedForSync)]
    [BepInDependency("com.bepis.r2api")]
    [BepInPlugin("com.KingEnderBrine.Mercenary2B","Mercenary2B","1.0.0")]
    public partial class Mercenary2BPlugin : BaseUnityPlugin
    {
        private static AssetBundle assetBundle;
        private static readonly List<Material> materialsWithRoRShader = new List<Material>();
        private void Awake()
        {
            BeforeAwake();
            using (var assetStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Mercenary2B.kingenderbrinemercenary2b"))
            {
                assetBundle = AssetBundle.LoadFromStream(assetStream);
            }

            On.RoR2.BodyCatalog.Init += BodyCatalogInit;

            ReplaceShaders();
            AddLanguageTokens();

            AfterAwake();
        }

        partial void BeforeAwake();
        partial void AfterAwake();
        static partial void BeforeBodyCatalogInit();
        static partial void AfterBodyCatalogInit();

        private static void ReplaceShaders()
        {
            materialsWithRoRShader.Add(LoadMaterialWithReplacedShader(@"Assets/Mercenary2B/Resources/Materials/mat2B.mat", @"Hopoo Games/Deferred/Standard"));
            materialsWithRoRShader.Add(LoadMaterialWithReplacedShader(@"Assets/Mercenary2B/Resources/Materials/matSword.mat", @"Hopoo Games/Deferred/Standard"));
        }

        private static Material LoadMaterialWithReplacedShader(string materialPath, string shaderName)
        {
            var material = assetBundle.LoadAsset<Material>(materialPath);
            material.shader = Shader.Find(shaderName);

            return material;
        }

        private static void AddLanguageTokens()
        {
            LanguageAPI.Add("KINGENDERBRINE_SKIN_MERCENARY2B_NAME", "2B");
            LanguageAPI.Add("KINGENDERBRINE_SKIN_MERCENARY2B_NAME", "2B", "en");
        }

        private static void BodyCatalogInit(On.RoR2.BodyCatalog.orig_Init orig)
        {
            orig();

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

                var renderers = bodyPrefab.GetComponentsInChildren<Renderer>(true);
                var skinController = bodyPrefab.GetComponentInChildren<ModelSkinController>();
                var mdl = skinController.gameObject;

                var skin = new LoadoutAPI.SkinDefInfo
                {
                    Icon = assetBundle.LoadAsset<Sprite>(@"Assets\SkinMods\Mercenary2B\Icons\Mercenary2BIcon.png"),
                    Name = skinName,
                    NameToken = "KINGENDERBRINE_SKIN_MERCENARY2B_NAME",
                    RootObject = mdl,
                    BaseSkins = Array.Empty<SkinDef>(),
                    UnlockableName = "",
                    GameObjectActivations = Array.Empty<SkinDef.GameObjectActivation>(),
                    RendererInfos = new CharacterModel.RendererInfo[]
                    {
                        new CharacterModel.RendererInfo
                        {
                            defaultMaterial = assetBundle.LoadAsset<Material>(@"Assets/Mercenary2B/Resources/Materials/mat2B.mat"),
                            defaultShadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On,
                            ignoreOverlays = false,
                            renderer = renderers[3]
                        },
                        new CharacterModel.RendererInfo
                        {
                            defaultMaterial = assetBundle.LoadAsset<Material>(@"Assets/Mercenary2B/Resources/Materials/matSword.mat"),
                            defaultShadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On,
                            ignoreOverlays = false,
                            renderer = renderers[4]
                        },
                    },
                    MeshReplacements = new SkinDef.MeshReplacement[]
                    {
                        new SkinDef.MeshReplacement
                        {
                            mesh = assetBundle.LoadAsset<Mesh>(@"Assets\SkinMods\Mercenary2B\Meshes\Nier2b.mesh"),
                            renderer = renderers[3]
                        },
                        new SkinDef.MeshReplacement
                        {
                            mesh = assetBundle.LoadAsset<Mesh>(@"Assets\SkinMods\Mercenary2B\Meshes\Sword.mesh"),
                            renderer = renderers[4]
                        },
                    },
                    MinionSkinReplacements = Array.Empty<SkinDef.MinionSkinReplacement>(),
                    ProjectileGhostReplacements = Array.Empty<SkinDef.ProjectileGhostReplacement>()
                };

                Array.Resize(ref skinController.skins, skinController.skins.Length + 1);
                skinController.skins[skinController.skins.Length - 1] = LoadoutAPI.CreateNewSkinDef(skin);

                var skinsField = typeof(BodyCatalog).GetFieldValue<SkinDef[][]>("skins");
                skinsField[BodyCatalog.FindBodyIndex(bodyPrefab)] = skinController.skins;
                MercBodyMercenary2BSkinAdded(skinController.skins[skinController.skins.Length - 1], bodyPrefab);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to add \"{skinName}\" skin to \"{bodyName}\"");
                Debug.LogError(e);
            }
        }
    }
}
