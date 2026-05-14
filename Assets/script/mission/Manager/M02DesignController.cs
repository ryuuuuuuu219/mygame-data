using System.Collections.Generic;
using UnityEngine;

public class M02DesignController : MonoBehaviour, IUAVStoragePresentation
{
    [System.Serializable]
    public struct StorageSet
    {
        public Vector3 position;
    }

    public int finalWaveId = 1;
    public float startDistance = 6000f;
    public float approachDistance = 2000f;
    public float storageAltitudeOffset = 3f;
    public float aaGunRadius = 300f;
    public string activatedStorageName = "UAV_Storage";

    public StorageSet[] storageSets =
    {
        new StorageSet { position = new Vector3(0f, 0f, 0f) },
        new StorageSet { position = new Vector3(-2800f, 0f, 2400f) },
        new StorageSet { position = new Vector3(2800f, 0f, 2400f) }
    };

    readonly UAVStorageMissionController storageMission = new();
    readonly List<Vector3> storagePositions = new();

    bool initialized;

    public void Initialize(SpawnTableManager manager, SpawnPlacementManager placement, GameObject playerObject)
    {
        if (initialized) return;

        storageMission.FinalWaveId = finalWaveId;
        storageMission.StartDistance = startDistance;
        storageMission.ApproachDistance = approachDistance;
        storageMission.StorageAltitudeOffset = storageAltitudeOffset;
        storageMission.AaGunRadius = aaGunRadius;
        storageMission.Initialize(manager, placement, playerObject, this);

        initialized = true;
    }

    public void StartWave(int waveId)
    {
        storagePositions.Clear();
        if (storageSets != null)
        {
            foreach (var set in storageSets)
                storagePositions.Add(set.position);
        }

        storageMission.StartWave(waveId, storagePositions);
    }

    public void OnUnknownActivatedAsStorage(GameObject storage, int previousWaveId, bool wasTransitionTarget)
    {
        if (storage != null)
            storage.name = activatedStorageName;
    }
}
