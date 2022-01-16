using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
public class PlayerController : MonoBehaviourPunCallbacks
{
    // Movement
    [SerializeField] private float jumpForce = 600f;                            // Amount of force added when the player jumps.
    [SerializeField] private LayerMask whatIsGround;                            // A mask determining what is ground to the character
    [SerializeField] private Transform groundCheck;                             // A position marking where to check if the player is grounded.
    [Range(0, .3f)] [SerializeField] private float movementSmoothing = .05f;    // How much to smooth out the movement
    const float groundCheckRadius = .02f;                                       // Radius of the overlap circle to determine if grounded
    Rigidbody2D rb2D;
    bool isGrounded = false;
    bool jump = false;
    float horizontalMove = 0f;
    Vector3 vel = Vector3.zero;
    bool facingRight = true;

    //Utilities
    public float runSpeed = 20f;
    public RectTransform lifeBar;
    public Animator anim;
    public bool airControl;

    int life = 100;

    private float attackTimer = 0;
    public GameObject attackObj;

    private bool didHit = false;

    PhotonView view;

    //Graphics
    int character = 0;

    private void Awake()
    {
        rb2D = GetComponent<Rigidbody2D>();
        view = GetComponent<PhotonView>();
    }

    void Start()
    {
        anim.ResetTrigger("Attack");
        // Since photon updates the position of online players by its transform we deactivate the rigidbody's gravity to avoid movement artifacts
        if (!view.IsMine)
        {
            rb2D.gravityScale = 0;
        }
        else
        {
            character = PlayerPrefs.GetInt("Character");
            anim.SetInteger("Character", character);
        }
    }

    private void FixedUpdate()
    {
        bool wasGrounded = isGrounded;
        isGrounded = false;

        // The player is grounded if a circlecast to the groundcheck position hits anything designated as ground
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

        // Resize lifebar
        lifeBar.sizeDelta = new Vector2(life, 10);
    }

    void Update()
    {
        if (view.IsMine)
        {
            // Check all inputs
            horizontalMove = Input.GetAxisRaw("Horizontal") * runSpeed;

            if (Input.GetButtonDown("Jump"))
            {
                jump = true;
            }

            // All attack behaviour is called from the animation
            if (attackTimer <= 0 && Input.GetButtonDown("Fire1"))
            {
                anim.SetTrigger("Attack");
                if (character == 1)
                {
                    GameObject go = PhotonNetwork.Instantiate("Bullet", attackObj.transform.position, Quaternion.identity);
                    go.GetComponent<Rigidbody2D>().AddForce((facingRight ? Vector2.right : Vector2.left) * 200);
                    go.GetComponent<BulletController>().shooterId = view.ViewID;
                }
            }
        }

        // Execute attack with collider
        if (attackTimer > 0)
        {
            if (character != 1)
            {
                attackObj.SetActive(true);
            }
            attackTimer -= Time.deltaTime;
            horizontalMove = 0;
            if (attackTimer <= 0)
            {
                attackObj.SetActive(false);
                didHit = false;
            }
        }

        // Check if player has fallen out of map
        if (transform.position.y < -15.0f)
        {
            transform.position = Vector3.zero;
        }
    }

    public void Attack()
    {
        // Start attack timer
        attackTimer = 0.4f;
    }

    public void Move(float move, bool jump)
    {
        // Only control the player if grounded or airControl is turned on
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
    }

    private void Flip()
    {
        // Switch the way the player is labelled as facing.
        facingRight = !facingRight;

        // Multiply the player's x local scale by -1.
        Vector3 theScale = transform.localScale;
        theScale.x *= -1;
        transform.localScale = theScale;

        // Multiply the lifeBar's x local scale by -1.
        Vector3 theLBScale = lifeBar.parent.localScale;
        theLBScale.x *= -1;
        lifeBar.parent.localScale = theLBScale;
    }

    [PunRPC]
    public void Damage(int damage)
    {
        // Take damage and start a "damage cooldown"
        life -= damage;
        if (life <= 0) Die();
    }

    [PunRPC]
    public void SetLife(int l)
    {
        life = l;
    }

    public void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.name.Contains("Bullet"))
        {
            BulletController bullet = collision.GetComponent<BulletController>();
            if(!bullet.destroying && bullet.shooterId != view.ViewID && bullet.shooterId != 0)
            {
                view.RPC("Damage", RpcTarget.All, 10);
                bullet.Destroy();
            }
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (view.IsMine)
        {
            if (collision.name.Contains("Attack"))
            {
                // Check if trigger is an attack
                if (!collision.gameObject.activeSelf || collision.transform.parent == transform) return;
                // Check "damage cooldown" and execute damage function
                if (!didHit)
                {
                    view.RPC("Damage", RpcTarget.All, 10);
                    attackTimer = 0.4f;
                    didHit = true;
                }
            }
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        if (view.IsMine)
        {
            character = PlayerPrefs.GetInt("Character");
            anim.SetInteger("Character", character);

            view.RPC("SetLife", RpcTarget.All, life);
        }
    }

    private void Die()
    {
        if (view.IsMine)
        {
            PhotonNetwork.Disconnect();
        }
    }
}
