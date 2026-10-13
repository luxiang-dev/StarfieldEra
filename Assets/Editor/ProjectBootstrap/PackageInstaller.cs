using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace ProjectBootstrap
{
    [InitializeOnLoad]
    public static class PackageInstaller
    {
        const string PendingKey = "StarfieldEra.Bootstrap.Pending";
        static AddAndRemoveRequest addRequest;
        static ListRequest listRequest;
        static double deadline;
        static PackageInstaller()
        {
            if (SessionState.GetBool(PendingKey, false))
                EditorApplication.delayCall += Resume;
        }
        public static void Install()
        {
            PlayerSettings.productName = "StarfieldEra";
            PlayerSettings.companyName = "StarfieldEra";
            PlayerSettings.bundleVersion = "0.1.0";
            SessionState.SetBool(PendingKey, true);
            addRequest = Client.AddAndRemove(new[] { "com.unity.cinemachine" });
            deadline = EditorApplication.timeSinceStartup + 600;
            EditorApplication.update += Poll;
        }
        static void Resume()
        {
            // Package installation can reload the scripting domain before its callback.
            listRequest = Client.List(true);
            deadline = EditorApplication.timeSinceStartup + 600;
            EditorApplication.update += Poll;
        }
        static void Poll()
        {
            if (EditorApplication.timeSinceStartup > deadline)
            {
                Finish(2, "Package resolution timed out.");
                return;
            }
            if (addRequest != null)
            {
                if (!addRequest.IsCompleted) return;
                if (addRequest.Status != StatusCode.Success)
                {
                    Finish(1, addRequest.Error?.message);
                    return;
                }
                addRequest = null;
                listRequest = Client.List(true);
            }
            if (listRequest == null || !listRequest.IsCompleted) return;
            if (listRequest.Status != StatusCode.Success)
            {
                Finish(1, listRequest.Error?.message);
                return;
            }
            if (!listRequest.Result.Any(p => p.name == "com.unity.cinemachine"))
            {
                Finish(1, "Cinemachine is missing after package resolution.");
                return;
            }
            Debug.Log("[Bootstrap] Resolved: " + string.Join(", ", listRequest.Result.Select(p => p.name + "@" + p.version)));
            AssetDatabase.SaveAssets();
            Finish(0, "Assets imported and saved.");
        }
        static void Finish(int code, string message)
        {
            EditorApplication.update -= Poll;
            SessionState.SetBool(PendingKey, false);
            if (code == 0) Debug.Log("[Bootstrap] " + message);
            else Debug.LogError("[Bootstrap] " + message);
            EditorApplication.Exit(code);
        }
    }
}
