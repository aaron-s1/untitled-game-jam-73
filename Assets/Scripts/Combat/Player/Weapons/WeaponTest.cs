using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponTest : MonoBehaviour, IFireable
{
    public void Fire(GameObject weaponPrefab)
    {
        Debug.Log("fired");
    }   
}