using System.Collections.Generic;
using UnityEngine;

public class Gun_e_pool : MonoBehaviour
{
    public GameObject bulletPrefab;
    public List<GameObject> bulletPool;
    public int poolSize1 = 500;

    public GameObject missilePrefab;
    public List<GameObject> missilePool;
    public int poolSize2 = 100;

    bool initialized;

    void Start()
    {
        EnsureInitialized();
    }

    void EnsureInitialized()
    {
        if (initialized) return;
        initialized = true;

        if (bulletPool == null)
            bulletPool = new List<GameObject>();
        if (bulletPrefab != null)
        {
            for (int i = bulletPool.Count; i < poolSize1; i++)
            {
                GameObject bullet = Instantiate(bulletPrefab);
                bullet.SetActive(false);
                bulletPool.Add(bullet);
            }
        }

        if (missilePool == null)
            missilePool = new List<GameObject>();
        if (missilePrefab != null)
        {
            for (int i = missilePool.Count; i < poolSize2; i++)
            {
                GameObject missile = Instantiate(missilePrefab);
                missile.SetActive(false);
                missilePool.Add(missile);
            }
        }

    }

    public GameObject missilepull(Vector3 StartPos, Vector3 velocity, float lifetime = 10f)
    {
        EnsureInitialized();
        if (missilePrefab == null) return null;

        foreach (GameObject missile in missilePool)
        {
            if (!missile.activeInHierarchy)
            {

                missile.GetComponent<Missile>().missileInit(StartPos, velocity, lifetime);

                return missile;
            }
        }
        GameObject newMissile = Instantiate(missilePrefab);
        newMissile.GetComponent<Missile>().missileInit(StartPos, velocity, lifetime);
        newMissile.SetActive(true);
        missilePool.Add(newMissile);
        return newMissile;
    }

    public GameObject bulletpull(float size,Vector3 StartPos, Vector3 velocity,float lifetime=3f)
    {
        EnsureInitialized();
        if (bulletPrefab == null) return null;

        foreach (GameObject bullet in bulletPool)
        {
            if (!bullet.activeInHierarchy)
            {
                bullet.transform.localScale = new Vector3(size, size, size);
                bullet.GetComponent<Gun_e>().lifeTime = lifetime;
                bullet.GetComponent<Gun_e>().velocity = velocity;
                bullet.transform.position = StartPos;
                bullet.SetActive(true);
                return bullet;
            }
        }

        GameObject newBullet = Instantiate(bulletPrefab);
        newBullet.transform.localScale = new Vector3(size, size, size);
        newBullet.GetComponent<Gun_e>().lifeTime = lifetime;
        newBullet.GetComponent<Gun_e>().velocity = velocity;
        newBullet.transform.position = StartPos;
        newBullet.SetActive(true);
        bulletPool.Add(newBullet);
        return newBullet;
    }

}
