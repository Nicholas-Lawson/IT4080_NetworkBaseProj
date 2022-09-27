using Unity.Netcode;
using UnityEngine;

public class Player : NetworkBehaviour {

    public NetworkVariable<Vector3> PositionChange = new NetworkVariable<Vector3>();
    public NetworkVariable<Vector3> RotationChange = new NetworkVariable<Vector3>();
    public NetworkVariable<Color> PlayerColor = new NetworkVariable<Color>(Color.red);
    private GameManager _gameMgr;
    private Camera _camera;

    public float movementSpeed = .5f;
    private float rotationSpeed = 1f;
    private BulletSpawner _bulletSpawner;

    public override void OnNetworkSpawn()
    {
        _camera = transform.Find("Camera").GetComponent<Camera>();
        if (IsOwner)
        {
            _gameMgr = GameObject.Find("GameManager").GetComponent<GameManager>();
            _gameMgr.RequestNewPlayerColorServerRpc();
        }
        _camera.enabled = IsOwner;
    }

    public void Start()
    {
        ApplyPlayerColor();
        PlayerColor.OnValueChanged += OnPlayerColorChanged;
        _bulletSpawner = transform.Find("RightArm").transform.Find("BulletSpawner").GetComponent<BulletSpawner>();
    }

    public void OnPlayerColorChanged(Color previous, Color current)
    {
        ApplyPlayerColor();
    }

    public void ApplyPlayerColor()
    {
        GetComponent<MeshRenderer>().material.color = PlayerColor.Value;
        transform.Find("LeftArm").GetComponent<MeshRenderer>().material.color = PlayerColor.Value;
        transform.Find("RightArm").GetComponent<MeshRenderer>().material.color = PlayerColor.Value;
    }

    private Vector3[] CalcMovement()
    {
        bool isShiftKeyDown = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        float x_move = 0.0f;
        float z_move = Input.GetAxis("Vertical");
        float y_rot = 0.0f;

        if (isShiftKeyDown)
        {
            x_move = Input.GetAxis("Horizontal");
        }
        else
        {
            y_rot = Input.GetAxis("Horizontal");
        }
        Vector3 moveVect = new Vector3(x_move, 0, z_move);
            moveVect *= movementSpeed;

        Vector3 rotVect = new Vector3(0, y_rot, 0);
            rotVect *= rotationSpeed;
        return new[] { moveVect, rotVect };
    }

    [ServerRpc]
    void RequestPositionForMovementServerRpc(Vector3 posChange, Vector3 rotChange)
    {
        if(!IsServer && !IsHost)
        {
            return;
        }

        PositionChange.Value = posChange;
        RotationChange.Value = rotChange;
    }


    //void RequestPositionForMovementServerRpc(Vector3 movement)
    //{
    //    Position.Value += movement;

    //    float planeSize = 5f;
    //    Vector3 newPosition = Position.Value + movement;

    //    newPosition.x = Mathf.Clamp(newPosition.x, planeSize * -1, planeSize);
    //    newPosition.z = Mathf.Clamp(newPosition.z, planeSize * -1, planeSize);
    //    Position.Value = newPosition;
    //}
    void Update()
    {
        if (IsOwner)
        {
            Vector3[] results = CalcMovement();
            RequestPositionForMovementServerRpc(results[0], results[1]);
            if(Input.GetButtonDown("Fire1"))
            {
                _bulletSpawner.FireServerRpc();
            }
        }
        if(!IsOwner || IsHost)
        {
            transform.Translate(PositionChange.Value);
            transform.Rotate(RotationChange.Value);
        }
    }
}
