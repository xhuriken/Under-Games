using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{

    /*
     * This script is an advanced point & click player controller.
     * When the player !isInMouvement, the camera can rotate around the player (with the mouse), but not moving !
     * When the player clic on something is clickable, the player move toward this object (isInMovement became true) and this is an animation !
     * The position to go to is stocked in every object clicked !
     * To know if an object is usable or a coord to go, it's stocked inside the object too !
     * After that, isInMovement became false again. and that is !
     * 
     * There will be another script we'll check when specific objects are clickable or not. (Like a manager you know) that it i think.
     */


    [Tooltip("Is the player currently in movement?")]
    [SerializeField] private bool isInMovement = false;
    [Tooltip("The speed at which the player moves.")]
    [SerializeField] private float movementSpeed = 5f;

    void Start()
    {
        
    }

    void Update()
    {

        if (Input.GetKeyDown(KeyCode.Mouse0))
        {

            //Launche raycast from mouse position

            //Get Mouse Position
            Vector3 mousePos = Input.mousePosition;
            Ray ray = Camera.main.ScreenPointToRay(mousePos);
            RaycastHit hit;
            
            //launch raycast
            if (Physics.Raycast(ray, out hit))
            {
                //Check if the object hit has a PointClickTarget component
                PointClickTarget target = hit.collider.GetComponent<PointClickTarget>();

                if (target != null) HandleClick(target);
            }
        }
    }


    public void HandleClick(PointClickTarget target)
    {

        Debug.Log("Player clicked on a PointClickTarget!");

    }
}
