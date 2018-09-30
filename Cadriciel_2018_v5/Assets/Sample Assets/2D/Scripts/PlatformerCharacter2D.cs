using UnityEngine;
using System;
using UnityEngine.UI;
using System.Collections;

public class PlatformerCharacter2D : MonoBehaviour 
{
	bool facingRight = true;							// For determining which way the player is currently facing.

	[SerializeField] float maxSpeed = 10f;				// The fastest the player can travel in the x axis.

	[SerializeField] float jumpSpeed = 15f;

	[SerializeField] float chargedJumpSpeed = 10f;

	[Range(0, 1)]
	[SerializeField] float crouchSpeed = .36f;			// Amount of maxSpeed applied to crouching movement. 1 = 100%
	
	[SerializeField] bool airControl = false;			// Whether or not a player can steer while jumping;
	[SerializeField] LayerMask whatIsGround;			// A mask determining what is ground to the character
	Transform groundCheck;								// A position marking where to check if the player is grounded.
	float groundedRadius = .2f;							// Radius of the overlap circle to determine if grounded
	bool grounded = false;								// Whether or not the player is grounded.
	
	Transform wallCheck;
	[SerializeField] LayerMask whatIsWall;
	float wallRadius = .2f;
	bool onWall = false;

	Transform ceilingCheck;								// A position marking where to check for ceilings
	float ceilingRadius = .01f;							// Radius of the overlap circle to determine if the player can stand up
	Animator anim;										// Reference to the player's animator component.

	long initialCrouchTs;

	[SerializeField] float maxChargeJumpTimeMs = 1000f;
	[SerializeField] float wallJumpDebounceMs = 200f;

	long initialWallJumpTs = 0;
	float jumpCharge = 0f;

	[SerializeField] Image jumpChargeBar;

	[SerializeField] GameObject jumpBarCanvas;


	[Range(0, UInt16.MaxValue)]
	[SerializeField] int maxJumps = 3;

	int currentJumps = 0;

	[SerializeField] Vector2 wallPushSpeedRight = new Vector2( 10f, 15f);

	[SerializeField] Vector2 wallPushSpeedLeft = new Vector2( -10f, 15f);

    void Awake()
	{
		// Setting up references.
		groundCheck = transform.Find("GroundCheck");
		ceilingCheck = transform.Find("CeilingCheck");
		wallCheck = transform.Find("WallCheck");

		anim = GetComponent<Animator>();
	}


	void FixedUpdate()
	{
		//Checks if the player is near and facing a wall
		onWall = Physics2D.OverlapCircle(wallCheck.position, wallRadius, whatIsWall);

		// The player is grounded if a circlecast to the groundcheck position hits anything designated as ground
		grounded = Physics2D.OverlapCircle(groundCheck.position, groundedRadius, whatIsGround);
		anim.SetBool("Ground", grounded);

		// Set the vertical animation
		anim.SetFloat("vSpeed", GetComponent<Rigidbody2D>().velocity.y);
	}


	public void Move(float move, bool crouch, bool jump)
	{
		bool wallJumpDebounce = true;

		if ((TicksToMs(System.DateTime.Now.Ticks) - initialWallJumpTs) > wallJumpDebounceMs)
		{
			wallJumpDebounce = false;
		}

		// If crouching, check to see if the character can stand up
		if(!crouch && anim.GetBool("Crouch"))
		{
			// If the character has a ceiling preventing them from standing up, keep them crouching
			if( Physics2D.OverlapCircle(ceilingCheck.position, ceilingRadius, whatIsGround))
				crouch = true;
		}

		//Character just started crouching
		if (!anim.GetBool("Crouch") && crouch && !jump && grounded)
		{
			initialCrouchTs = TicksToMs(System.DateTime.Now.Ticks);	
			jumpCharge = 0;
			jumpBarCanvas.SetActive(true);
		}
		else if (anim.GetBool("Crouch") && crouch)
		{
			jumpCharge = Math.Min((TicksToMs(System.DateTime.Now.Ticks) - initialCrouchTs) / maxChargeJumpTimeMs, 1);
			jumpChargeBar.fillAmount = jumpCharge;
		}
		else if (!crouch)
		{
			jumpCharge = 0;
			jumpBarCanvas.SetActive(false);
		}
		// Set whether or not the character is crouching in the animator
		anim.SetBool("Crouch", crouch && grounded);

		if (!wallJumpDebounce)
		{
			//only control the player if grounded or airControl is turned on
			if(grounded || airControl)
			{
				// Reduce the speed if crouching by the crouchSpeed multiplier
				move = (crouch ? move * crouchSpeed : move);

				// The Speed animator parameter is set to the absolute value of the horizontal input.
				anim.SetFloat("Speed", Mathf.Abs(move));

				// Move the character
				GetComponent<Rigidbody2D>().velocity = new Vector2(move * maxSpeed, GetComponent<Rigidbody2D>().velocity.y);

				// If the input is moving the player right and the player is facing left...
				if(move > 0 && !facingRight)
					// ... flip the player.
					Flip();
				// Otherwise if the input is moving the player left and the player is facing right...
				else if(move < 0 && facingRight)
					// ... flip the player.
					Flip();
			}

			// If the player should jump...
			if (grounded && jump) {
				// Add a vertical force to the player.
				anim.SetBool("Ground", false);
				jumpBarCanvas.SetActive(false);
				currentJumps = 1;

				GetComponent<Rigidbody2D>().velocity = new Vector2(0, jumpSpeed + jumpCharge * chargedJumpSpeed);
			}
			else if (!grounded && onWall && jump)
			{
				initialWallJumpTs = TicksToMs(System.DateTime.Now.Ticks);

				if (facingRight)
				{
					GetComponent<Rigidbody2D>().velocity = wallPushSpeedLeft;
				}
				else
				{
					GetComponent<Rigidbody2D>().velocity = wallPushSpeedRight;
				}
				Flip();
			}
			else if (!grounded && jump && currentJumps < maxJumps)
			{
				currentJumps++;
				GetComponent<Rigidbody2D>().velocity = new Vector2(0, jumpSpeed);
			}
		}
	}

	
	void Flip ()
	{
		// Switch the way the player is labelled as facing.
		facingRight = !facingRight;
		
		// Multiply the player's x local scale by -1.
		Vector3 theScale = transform.localScale;
		theScale.x *= -1;
		transform.localScale = theScale;
	}

	private long TicksToMs(long ticks)
	{
		return ticks / TimeSpan.TicksPerMillisecond;
	}
}
