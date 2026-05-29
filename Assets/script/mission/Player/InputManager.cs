using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// プレイヤー／AI共通の入力集約クラス
/// Input.GetAxis() を一元管理して、他のスクリプトから値を取得する
/// </summary>
public class InputManager : MonoBehaviour
{
    public static InputManager Instance;  // シングルトン的アクセス

    public InputActionAsset inputActions; // ← この中に「Player」マップが入っている
    private InputActionMap playerMap;
    private InputAction rollAction;
    private InputAction pitchAction;
    private InputAction viewAction;
    private InputAction l1Action;
    private InputAction r1Action;
    private InputAction l2Action;
    private InputAction r2Action;
    private InputAction l3Action;
    private InputAction r3Action;
    private InputAction cancelAction;
    private InputAction submitAction;
    private InputAction squareAction;
    private InputAction triangleAction;
    private InputAction menuAction;
    private InputAction upAction;
    private InputAction downAction;
    private InputAction leftAction;
    private InputAction rightAction;

    [Header("Stick Axes")]
    public float horizontalL; // 左スティックX
    public float verticalL;   // 左スティックY
    public float horizontalR; // 右スティックX
    public float verticalR;   // 右スティックY
    public float accel;       // アクセル

    [Header("Triggers")]
    public float l2;
    public float r2;

    [Header("RButtons")]
    public bool submit;
    public bool cancel;

    public bool fireGun;
    public bool fireMissile;
    public bool changeWeapon;
    public bool targetChange;

    public bool north;
    public bool south;
    public bool west;
    public bool east;

    [Header("LButtons")]
    public bool up;
    public bool down;
    public bool left;
    public bool right;

    public bool stickL;
    public bool stickR;

    public bool l1;
    public bool altl2;
    public bool r1;
    public bool altr2;

    public bool menu;//10.15の目標

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void OnEnable()
    {
        playerMap = inputActions.FindActionMap("Player");
        rollAction = playerMap.FindAction("Roll");
        pitchAction = playerMap.FindAction("Pitch");
        viewAction = playerMap.FindAction("View");
        l1Action = playerMap.FindAction("L1");
        r1Action = playerMap.FindAction("R1");
        l2Action = playerMap.FindAction("L2");
        r2Action = playerMap.FindAction("R2");
        l3Action = playerMap.FindAction("L3");
        r3Action = playerMap.FindAction("R3");
        cancelAction = playerMap.FindAction("Cancel");
        submitAction = playerMap.FindAction("Submit");
        squareAction = playerMap.FindAction("Square");
        triangleAction = playerMap.FindAction("Triangle");
        menuAction = playerMap.FindAction("menu");
        upAction = playerMap.FindAction("Up");
        downAction = playerMap.FindAction("Down");
        leftAction = playerMap.FindAction("Left");
        rightAction = playerMap.FindAction("Right");

        playerMap.Enable();
    }

    void Update()
    {
        // ---- スティック ----
        horizontalL = rollAction.ReadValue<float>();
        verticalL = pitchAction.ReadValue<float>();

        // 修正: ReadValue<Vector2>() の使用方法を修正
        Vector2 viewInput = viewAction.ReadValue<Vector2>();
        horizontalR = viewInput.x;
        verticalR = viewInput.y;

        l1 = l1Action.IsPressed();
        r1 = r1Action.IsPressed();
        accel = (r1 ? 1f : 0f) + (l1 ? -1f : 0f);

        // ---- ボタン ----
        altl2 = l2Action.IsPressed();
        altr2 = r2Action.IsPressed();

        stickL = l3Action.IsPressed();
        stickR = r3Action.IsPressed();

        cancel = cancelAction.WasPressedThisFrame();
        submit = submitAction.WasPressedThisFrame();

        fireGun = cancelAction.IsPressed();
        fireMissile = submitAction.WasPressedThisFrame();

        changeWeapon = squareAction.WasPressedThisFrame();
        targetChange = triangleAction.WasPressedThisFrame();
        menu = menuAction.WasPressedThisFrame();


        up = upAction.IsPressed();
        down = downAction.IsPressed();
        left = leftAction.IsPressed();
        right = rightAction.IsPressed();

        north = triangleAction.IsPressed();
        south = cancelAction.IsPressed();
        west = squareAction.IsPressed();
        east = submitAction.IsPressed();

    }
}
