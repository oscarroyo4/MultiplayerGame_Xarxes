using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(NetworkClient))]
[RequireComponent(typeof(NetworkClientDisplay))]
public class NetworkInputSync : MonoBehaviour
{
    [Tooltip("The distance to be moved in each move input")]
    [SerializeField] private float jumpForce = 600f;                            // Amount of force added when the player jumps.
    [SerializeField] private LayerMask whatIsGround;                            // A mask determining what is ground to the character
    [SerializeField] private Transform groundCheck;                             // A position marking where to check if the player is grounded.
    [Range(0, .3f)] [SerializeField] private float movementSmoothing = .05f;  // How much to smooth out the movement
    const float groundCheckRadius = .02f;                                       // Radius of the overlap circle to determine if grounded

    int life = 100;
    Rigidbody2D rb2D;
    bool isGrounded = false;
    bool jump = false;
    float horizontalMove = 0f;
    Vector3 vel = Vector3.zero;
    bool facingRight = true;

    public float runSpeed = 20f;
    public RectTransform lifeBar;
    public Animator anim;
    public bool airControl;

    private float attackTimer = 0;
    private Vector2 attackSize = new Vector2(1.2f, 0.5f);
    private Vector3 attackPosLeft = new Vector3(-1.1f, 0.35f, 0);
    private Vector3 attackPosRight = new Vector3(1.1f, 0.35f, 0);

    NetworkClient client;
    float startTimer = 0.5f;

    private void Awake()
    {
        rb2D = GetComponent<Rigidbody2D>();
        client = GetComponent<NetworkClient>();
    }

    void Start()
    {
        anim.ResetTrigger("Attack");
    }

    private void FixedUpdate()
    {
        bool wasGrounded = isGrounded;
        isGrounded = false;

        // The player is grounded if a circlecast to the groundcheck position hits anything designated as ground
        // This can be done using layers instead but Sample Assets will not overwrite your project settings.
        Collider2D[] colliders = Physics2D.OverlapCircleAll(groundCheck.position, groundCheckRadius, whatIsGround);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i].gameObject != gameObject)
            {
                isGrounded = true;
            }
        }

        // Move our character
        Move(horizontalMove * Time.fixedDeltaTime, jump);
        jump = false;

        lifeBar.sizeDelta = new Vector2(life, 10);
    }

    void Update()
    {
        if (startTimer > 0)
        {
            startTimer -= Time.deltaTime;
            return;
        }
        if (client.id != "")
        {
            string action = "";

            horizontalMove = Input.GetAxisRaw("Horizontal") * runSpeed;

            if (Input.GetButtonDown("Jump"))
            {
                jump = true;
                action = "jump";
            }

            if (Input.GetButtonDown("Fire1"))
            {
                Attack();
            }

            if (attackTimer > 0)
            {
                Collider2D[] hitColliders;
                if (facingRight) hitColliders = Physics2D.OverlapBoxAll(gameObject.transform.localPosition + attackPosRight, attackSize, 0);
                else hitColliders = Physics2D.OverlapBoxAll(gameObject.transform.localPosition + attackPosLeft, attackSize, 0);
                attackTimer -= Time.deltaTime;
                horizontalMove = 0;
                int i = 0;
                action = "attacking";
                //Check when there is a new collider coming into contact with the box
                while (i < hitColliders.Length)
                {
                    //Output all of the collider names
                    Debug.Log("Hit : " + hitColliders[i].name);
                    action = hitColliders[i].name;
                    //Increase the number of Colliders in the array
                    i++;
                    attackTimer = 0;
                }
            }

            if (transform.position.y < -8.0f) { transform.position = Vector3.zero; action = "dead"; }

            //Send client data
            client.SendPacket(life, action);
        }
    }

    void Attack()
    {
        anim.SetTrigger("Attack");

        attackTimer = 0.4f;
    }

    public void Move(float move, bool jump)
    {
        //only control the player if grounded or airControl is turned on
        if (isGrounded || airControl)
        {
            // Move the character by finding the target velocity
            Vector3 targetVelocity = new Vector2(move * 10f, rb2D.velocity.y);
            // And then smoothing it out and applying it to the character
            rb2D.velocity = Vector3.SmoothDamp(rb2D.velocity, targetVelocity, ref vel, movementSmoothing);

            if (move != 0) anim.SetBool("Running", true);
            else anim.SetBool("Running", false);

            // If the input is moving the player right and the player is facing left...
            if (move > 0 && !facingRight)
            {
                // ... flip the player.
                Flip();
            }
            // Otherwise if the input is moving the player left and the player is facing right...
            else if (move < 0 && facingRight)
            {
                // ... flip the player.
                Flip();
            }
        }
        // If the player should jump...
        if (isGrounded && jump)
        {
            // Add a vertical force to the player.
            isGrounded = false;
            rb2D.AddForce(new Vector2(0f, jumpForce));
        }

        client.desiredPosition = transform.position;
    }

    private void Flip()
    {
        // Switch the way the player is labelled as facing.
        facingRight = !facingRight;

        // Multiply the player's x local scale by -1.
        Vector3 theScale = transform.localScale;
        theScale.x *= -1;
        transform.localScale = theScale;
    }

    public void Damage(int damage)
    {
        life -= damage;
    }
}
