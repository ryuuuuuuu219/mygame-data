using System.Collections.Generic;
using UnityEngine;

public class Gun_p_pool : MonoBehaviour
{
    public GameObject bulletPrefab;
    public List<GameObject> bulletPool;
    public int poolSize1 = 500;

    public GameObject missilePrefab;
    public List<GameObject> missilePool;
    public int poolSize2 = 100;


    void Start()
    {

        bulletPool = new List<GameObject>();
        for (int i = 0; i < poolSize1; i++)
        {
            GameObject bullet = Instantiate(bulletPrefab);
            bullet.SetActive(false);
            bulletPool.Add(bullet);
        }

        missilePool = new List<GameObject>();
        for (int i = 0; i < poolSize2; i++)
        {
            GameObject missile = Instantiate(missilePrefab);
            missile.SetActive(false);
            missilePool.Add(missile);
        }

    }

    public GameObject missilepull(Vector3 StartPos, Vector3 velocity, float lifetime = 10f)
    {
        foreach (GameObject missile in missilePool)
        {
            if (!missile.activeInHierarchy)
            {

                missile.GetComponent<Missile_p>().missileInit(StartPos, velocity, lifetime);

                return missile;
            }
        }
        GameObject newMissile = Instantiate(missilePrefab);
        newMissile.GetComponent<Missile_p>().missileInit(StartPos, velocity, lifetime);
        newMissile.SetActive(true);
        missilePool.Add(newMissile);
        return newMissile;
    }

    public GameObject bulletpull(float size,Vector3 StartPos, Vector3 velocity,float lifetime=3f)
    {
        foreach (GameObject bullet in bulletPool)
        {
            if (!bullet.activeInHierarchy)
            {
                bullet.transform.localScale = new Vector3(size, size, size);
                bullet.GetComponent<Gun_p>().lifeTime = lifetime;
                bullet.GetComponent<Gun_p>().velocity = velocity;
                bullet.transform.position = StartPos;
                bullet.SetActive(true);
                return bullet;
            }
        }

        GameObject newBullet = Instantiate(bulletPrefab);
        newBullet.transform.localScale = new Vector3(size, size, size);
        newBullet.GetComponent<Gun_p>().lifeTime = lifetime;
        newBullet.GetComponent<Gun_p>().velocity = velocity;
        newBullet.transform.position = StartPos;
        newBullet.SetActive(true);
        bulletPool.Add(newBullet);
        return newBullet;
    }

}
