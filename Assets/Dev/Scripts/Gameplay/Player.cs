using DG.Tweening;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

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

    [Header("Settings")]
    [SerializeField] private Camera _camera;

    [Header("Animation")]
    [Tooltip("Is the player currently in movement?")]
    [SerializeField] private bool isInMovement = false;
    [Tooltip("The speed at which the player moves.")]
    [SerializeField] private float movementSpeed = 5f;

    [Header("Look")]
    [SerializeField] private float minStep = 1f;
    [SerializeField] private float maxStep = 5f;
    [Tooltip("Mouse sensitivity for camera rotation.")]
    [SerializeField] private float lookSensitivity = 3f;

    [Tooltip("Clamp for vertical rotation (pitch).")]
    [SerializeField] private float minPitch = -30f;
    [SerializeField] private float maxPitch = 100f;

    [Tooltip("DOTween smoothing duration for look rotation.")]
    [SerializeField] [MinMaxSlider(0f, 0.5f)] private float lookTweenDuration = 0.08f;

    private float _yaw;
    private float _pitch;
    private Tween _lookTween;

    void Start()
    {
        _camera = GetComponentInChildren<Camera>();

        Vector3 euler = _camera.transform.rotation.eulerAngles;
        _yaw = euler.y;
        _pitch = euler.x;
    }

    void Update()
    {

        if (!isInMovement) HandleLook();

        
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

    /// <summary>
    /// Whenused click on an PointClickTarget, we process it
    /// </summary>
    /// <param name="target"></param>
    public void HandleClick(PointClickTarget target)
    {
        Debug.Log($"Player clicked on a PointClickTarget! {target.gameObject.name}");
        //GameObject ob = target.gameObject;

        switch (target.InteractionType)
        {
            case InteractionType.MoveTo:
                // We had clicked on a point to go to ! 


                Move(target);


                return;
            case InteractionType.Object:
                // We had clicked on a object to use


                return;
            default:
                Debug.Log("I'm gay");
                return;
        }
    }

    public void Move(PointClickTarget target)
    {
        if (isInMovement) return;
        isInMovement = true;

        Transform targetTransform = target.GetTransformTarget();

        Transform cam = _camera.transform;

        cam.DOKill();

        Sequence seq = DOTween.Sequence();
        
        //TODO: Polish it !
        seq.Join(cam.DOMove(targetTransform.position, 0.5f));

        seq.Join(cam.DORotateQuaternion(targetTransform.rotation, 0.5f));

        seq.OnComplete(() =>
        {
            isInMovement = false;
        });
    }


    public void HandleLook()
    {
        // Process the look

        float mx = Input.GetAxis("Mouse X");
        float my = Input.GetAxis("Mouse Y");

        // Update yaw/pitch
        _yaw += mx * lookSensitivity;
        _pitch -= my * lookSensitivity;

        // Clamp pitch to avoid flipping
        _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);

        // Compute target rotation (camera orbits around player)
        Quaternion targetRot = Quaternion.Euler(_pitch, _yaw, 0f);

        // Smooth it with DOTween (kill previous tween to avoid stacking)
        _lookTween?.Kill();
        _lookTween = _camera.transform.DORotateQuaternion(targetRot, lookTweenDuration);

    }
}
