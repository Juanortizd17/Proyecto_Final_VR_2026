using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace MixLab.EditorTools
{
    /// <summary>
    /// Capa VR del bar (Meta Quest con XR Interaction Toolkit). Se arma en la misma escena que el PC:
    /// XR Origin de los Starter Assets (manos, locomoción, teletransporte, rayos), botellas y vaso
    /// agarrables, palancas que se arrastran, posavasos con socket y zona de teletransporte.
    /// Todo empieza apagado; <see cref="MixVrRig"/> lo enciende si hay visor.
    /// </summary>
    public static partial class MixSceneBuilder
    {
        const string RigPrefab = "Assets/Samples/XR Interaction Toolkit/3.0.8/Starter Assets/Prefabs/XR Origin (XR Rig).prefab";

        static void BuildVr(Transform managers, GameObject desktopPlayer, GameObject desktopHud, EventSystem es,
                            MixGlass glass, TemperatureLever lever)
        {
            var root = new GameObject("--- VR (Meta Quest) ---").transform;
            var behaviours = new List<Behaviour>();
            var objects = new List<GameObject>();

            new GameObject("XR Interaction Manager", typeof(XRInteractionManager)).transform.SetParent(root);

            // Rig del jugador: mismo puesto y orientación que el jugador de PC
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RigPrefab);
            GameObject rig = null; Camera cam = null;
            if (prefab == null) Debug.LogWarning("[MixSceneBuilder] Falta el XR Origin de los Starter Assets: " + RigPrefab);
            else
            {
                rig = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                rig.transform.SetParent(root);
                rig.transform.SetPositionAndRotation(PlayerStart, Quaternion.identity);
                cam = rig.GetComponentInChildren<Camera>(true);
                if (cam)
                {
                    cam.nearClipPlane = 0.02f; cam.farClipPlane = 40f;
                    cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.05f, 0.04f, 0.09f);
                }
                rig.SetActive(false);
            }

            // Botones de los tableros con los rayos de los controles
            var xrUi = es.gameObject.AddComponent<XRUIInputModule>();
            xrUi.enabled = false;

            // Ingredientes: se agarran y se vierten inclinándolos sobre el vaso
            foreach (var bottle in Object.FindObjectsByType<Bottle>(FindObjectsSortMode.InstanceID))
            {
                var grab = AddGrab(bottle.gameObject);
                var pour = bottle.gameObject.AddComponent<VrPourable>();
                pour.Setup(grab); pour.enabled = false;
                behaviours.Add(grab); behaviours.Add(pour);
            }

            // Vaso: se agarra, se agita sacudiéndolo, se vacía volteándolo
            var glassGrab = AddGrab(glass.gameObject);
            var vrGlass = glass.gameObject.AddComponent<VrGlass>();
            vrGlass.Setup(glassGrab); vrGlass.enabled = false;
            behaviours.Add(glassGrab); behaviours.Add(vrGlass);

            // Palancas: se agarran y se arrastran
            foreach (var t in new[] { lever.gameObject, Object.FindFirstObjectByType<ShakeLever>().gameObject })
            {
                var simple = t.AddComponent<XRSimpleInteractable>();
                simple.enabled = false;
                var vl = t.AddComponent<VrLever>();
                vl.Setup(simple, t.GetComponent<TemperatureLever>(), t.GetComponent<ShakeLever>());
                vl.enabled = false;
                behaviours.Add(simple); behaviours.Add(vl);
            }

            // Posavasos del cliente: socket que sirve la bebida al encajar el vaso
            var coaster = GameObject.Find("Coaster");
            var socketGo = new GameObject("CoasterSocket");
            socketGo.transform.SetParent(root);
            socketGo.transform.position = coaster ? coaster.transform.position + Vector3.up * 0.004f : new Vector3(0f, Top, 1.02f);
            var trigger = socketGo.AddComponent<SphereCollider>();
            trigger.isTrigger = true; trigger.radius = 0.13f; trigger.center = new Vector3(0f, 0.07f, 0f);
            var socket = socketGo.AddComponent<CoasterSocket>();
            socket.SetGlass(glass);
            socket.enabled = false;
            behaviours.Add(socket);

            // Zona de teletransporte detrás de la barra (invisible, solo en VR)
            var zone = new GameObject("VrTeleportZone");
            zone.transform.SetParent(root);
            zone.transform.position = new Vector3(0f, -0.01f, -0.25f);
            var box = zone.AddComponent<BoxCollider>(); box.size = new Vector3(3.8f, 0.02f, 1.4f);
            zone.AddComponent<TeleportationArea>();
            zone.SetActive(false);
            objects.Add(zone);

            var tutorial = Object.FindFirstObjectByType<MixTutorial>();
            var vr = new GameObject("MixVrRig");
            vr.transform.SetParent(managers);
            vr.AddComponent<MixVrRig>().Setup(rig, cam, desktopPlayer, desktopHud, es.GetComponent<InputSystemUIInputModule>(),
                xrUi, tutorial, behaviours.ToArray(), objects.ToArray());
        }

        static XRGrabInteractable AddGrab(GameObject go)
        {
            var rb = go.GetComponent<Rigidbody>();
            if (rb == null) rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true; rb.useGravity = false;
            rb.interpolation = RigidbodyInterpolation.None;
            var grab = go.AddComponent<XRGrabInteractable>();
            grab.movementType = XRBaseInteractable.MovementType.Instantaneous;
            grab.throwOnDetach = false;
            grab.useDynamicAttach = true;
            grab.retainTransformParent = true;
            grab.enabled = false;
            return grab;
        }
    }
}
