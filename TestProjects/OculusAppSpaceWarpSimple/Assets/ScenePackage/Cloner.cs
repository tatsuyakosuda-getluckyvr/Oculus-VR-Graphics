using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cloner : MonoBehaviour
{

    [SerializeField] private GameObject _prefab = default;

    private void Start()
    {
        for (int i = 0; i < 10; i++)
        {
            for (int j = 0; j < 10; j++)
            {
                Instantiate(_prefab, new Vector3(i, 0, j) * 2.2f, Quaternion.identity, transform);
            }
        }

    }

}
