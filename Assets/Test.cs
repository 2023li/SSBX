using System.Collections;
using System.Collections.Generic;
using SSBX;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;
using UnityEngine.UI;

public class Test : MonoBehaviour
{
    private Button btn1;
    public Building go1;
    public BuildingConfig bc1;


    private BuildControllerV2 buildController;

    // Start is called before the first frame update
    void Start()
    {
        buildController = GameObject.FindAnyObjectByType<BuildControllerV2>();
        btn1 = gameObject.GetComponent<Button>();

        btn1.onClick.AddListener(() =>
        {
            buildController.EnterPlaceMode(go1, bc1);
        });
      

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
