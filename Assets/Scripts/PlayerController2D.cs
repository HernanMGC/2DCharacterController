using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController2D : MonoBehaviour
{
    // PUBLIC
    #region Initial variables
    // Walk
    [Header("Character Movement: Walk")]
    [Tooltip("Maximum speed The character can walk.")]
    public float maxWalkSpeed = 8.0f;
    [Tooltip("The character's walk acceleration.")]
    public float walkAcceleration = 6.0f;
    [Tooltip("Maximum angle the The character can walk over.")]
    public float maxSlopeAngle = 60.0f;
    [Space(0)]
   
    // Jump
    [Header("Character Movement: Jump")]
    [Tooltip("Set the maximum number of jumps the character can perform without touching the ground.")]
    public int jumpMaxCount = 2;
    [Tooltip("Maximum height the character can reach with a single jump.")]
    public float jumpMaxHeight = 4.0f;
    [Tooltip("The time it takes for the character to reach the jumpMaxHeight.")]
    public float jumpTimeToMaxHeight = 0.5f;
    [Tooltip("Gravity multiplier applied to the character when apex of the jump is reached.")]
    public float jumpFinishGravityMultiplier = 3.0f;
    [Tooltip("Flag the enable/disable the double jump when falling from a higher ground.")]
    public bool allowDoubleJumpWhenFalling = false;
    [Tooltip("Flag the enable/disable the hold jump button. If disabled the character will reach the jumpMaxHeight no matter how long the player press the button and jumpFinishGravityMultiplier will have no effect on gravity.")]
    public bool allowHoldButtonToJumpHigher = false;
    [Space(0)]

    // Probing
    [Header("Collision detection")]
    [Tooltip("Sets the maximum collision probing distance.")]
    public float probingMaxDistance = 5.0f;
    [Tooltip("Ground check distance offset on Y axis.")]
    public float groundOffsetY = 0.01f;
    [Tooltip("Ground check distance offset on X axis.")]
    public float groundOffsetX = 0.01f;
    [Tooltip("Ground check distance offset on X axis when the character is over a slope.")]
    public float groundSlopeOffsetY = 0.05f;
    [Tooltip("Collision probing ray launch point offset on X axis for bottom probing.")]
    public float rayBottomOffsetX = 0.005f;
    [Tooltip("Collision probing ray launch point offset on Y axis for bottom probing.")]
    public float rayBottomOffsetY = 0.3f;
    [Tooltip("Collision probing ray launch point offset on X axis for top probing.")]
    public float rayTopOffsetX = 0.005f;
    [Tooltip("Collision probing ray launch point offset on Y axis for top probing.")]
    public float rayTopOffsetY = 0.3f;
    [Tooltip("Collision probing ray launch point offset on X axis for sides probing.")]
    public float raySideOffsetX = 0.05f;
    [Tooltip("Collision probing ray launch point offset on Y axis for sides probing.")]
    public float raySideOffsetY = 0.05f;
    #endregion

    // PRIVATE
    // Walk
    #region Private walk variables
    private float currentSpeedX;
    private float currentSpeedY;
    private float currentMaxSpeedX;
    #endregion

    // Jump
    #region Private jump variables
    private float jumpInitialVelocity;
    private int jumpCount = 0;
    private float jumpGravity;
    private float jumpFinishGravity;
    private float currentGravity;
    #endregion

    // Movement states
    #region Private states variables
    private Vector2 amountTomve;
    private bool isRunning = false;
    private bool isJumpButtonPressed = false;
    private bool isJumpButtonReleased = false;
    private bool isJumping = false;
    private float movementDir;
    #endregion

    // Probing
    #region Private collision variables
    private Vector3 bottomLeftCorner;
    private Vector3 bottomRightCorner;
    private Vector3 bottomCenterCorner;
    private Vector3 topLeftCorner;
    private Vector3 topRightCorner;
    private Vector3 topCenterCorner;
    private Vector3 sideTopCorner;
    private Vector3 sideBottomCorner;
    private Vector3 sideCenterCorner;
    private Vector3 colliderSize;

    private bool isOnSlope = false;
    private bool isAboveSlope = false;
    private bool isAboveSlopeL = false;
    private bool isAboveSlopeR = false;
    private bool isOnNonWalkableSlope = false;
    private bool isOnNonWalkableSlopeL = false;
    private bool isOnNonWalkableSlopeR = false;
    private bool isOnNonWalkableSlopeC = false;

    private bool isGrounded = false;
    private bool isGroundedL = false;
    private bool isGroundedR = false;
    private bool isGroundedC = false;

    private bool isBumpedTop = false;
    private bool isBumpedTopL = false;
    private bool isBumpedTopR = false;
    private bool isBumpedTopC = false;

    private bool isBumpedSide = false;
    private bool isBumpedSideT = false;
    private bool isBumpedSideB = false;
    private bool isBumpedSideC = false;
    #endregion

    // Debugging
    #region Private collision variables
    private float debugLineLifetime = 0.01f;
    #endregion

    #region Unity callbacks
    void Start()
    {
        colliderSize = GetComponent<Collider>().bounds.size;
        jumpGravity = 2 * jumpMaxHeight / (jumpTimeToMaxHeight * jumpTimeToMaxHeight);
        jumpFinishGravity = jumpGravity * jumpFinishGravityMultiplier;
        currentGravity = jumpGravity;
        jumpInitialVelocity = 2 * jumpMaxHeight / jumpTimeToMaxHeight;
    }

    void Update()
    {
        // Check if Character should jump
        isJumpButtonPressed = Input.GetButtonDown("Jump");
        if (isJumpButtonPressed) {
            Jump();
        }

        // Check if Character hold jump is active and stops jump if button is released
        if (allowHoldButtonToJumpHigher) {
            isJumpButtonReleased = Input.GetButtonUp("Jump");
            if (isJumpButtonReleased) {
                StopJump();
            }
        }

        //Calculates Speed components and update Move Vector
        currentMaxSpeedX = Input.GetAxisRaw("Horizontal") * maxWalkSpeed;
        currentSpeedX = GetCurrentSpeedX(currentSpeedX, currentMaxSpeedX, walkAcceleration);
        currentSpeedY = GetCurrentSpeedY(currentSpeedY, currentGravity);

        Vector2 moveVector = new Vector2();
        moveVector.x = currentSpeedX * Time.deltaTime;
        moveVector.y = currentSpeedY * Time.deltaTime;

        isRunning = moveVector.x != 0;
        
        UpdateCornerPosition(moveVector);
        TryToMove(moveVector);
    }
    #endregion

    #region Speed calculcations
    private float GetCurrentSpeedX(float speed, float maxSpeed, float acceleration)
    {
        movementDir = Mathf.Sign(maxSpeed - speed);

        // Check if Max Speed has been reached to prevent unnecesary operations
        if (speed != maxSpeed) {
            speed += acceleration * Time.deltaTime * movementDir;
            speed = (movementDir == Mathf.Sign(maxSpeed - speed)) ? speed : maxSpeed;
        }

        return speed;
    }

    private float GetCurrentSpeedY(float speed, float currentGravity)
    {
        // Check if Character has fallen on the ground and is not jumping through a platform
        if (isGrounded && currentSpeedY <= 0.0f) {
            speed = 0.0f;

        // If Character is not still on the ground apply gravity
        } else {
            speed = speed - currentGravity * Time.deltaTime;
        }
 
        return speed;
    }
    #endregion

    #region Movement
    private void TryToMove(Vector2 moveVector)
    {
        // Probe downward, sides and upward to update Move Vector to posible values
        moveVector = ProbeDownward(moveVector);

        // Prevent sides check when char is not running
        if (isRunning) {
            moveVector = ProbeSides(moveVector);
        }

        // Prevent upward check when char is not jumping
        if (currentSpeedY > 0) {
            moveVector = ProbeUpward(moveVector);
        }

        // Update jump status according to movement states
        if (isGrounded || isOnSlope) {
            jumpCount = 0;
            currentGravity = jumpGravity;
            isJumping = false;

        // Prevents double jump when falling without jumping
        } else if (currentSpeedY < 0.0f && !isJumping && jumpCount == 0 && !allowDoubleJumpWhenFalling) {
            jumpCount = 1;
        }

        transform.position = transform.position + new Vector3(moveVector.x, moveVector.y, 0.0f);
    }
    #endregion

    #region Jump
    private void Jump()
    {
        // Update movement states when jump is pressed
        if (jumpCount < jumpMaxCount) {
            isJumping = true;
            currentGravity = jumpGravity;
            isGrounded = false;
            isOnSlope = false;
            currentSpeedY = jumpInitialVelocity;
            jumpCount++;

        } else if (isGrounded) {
            jumpCount = 0;
        }
    }

    private void StopJump()
    {
        if (isJumping) {
            currentGravity = jumpFinishGravity;
        }
    }
    #endregion

    #region Collsion detection
    private Vector3 ProbeDownward(Vector3 moveVector)
    {
        float nextBottomPosition = bottomCenterCorner.y + moveVector.y - rayBottomOffsetY;
        float bottomPositionL = nextBottomPosition;
        float bottomPositionR = nextBottomPosition;
        float bottomPositionC = nextBottomPosition;

        RaycastHit leftHit;
        if (Physics.Raycast(bottomLeftCorner, transform.TransformDirection(Vector3.down), out leftHit, probingMaxDistance)) {
            Debug.DrawRay(bottomLeftCorner, transform.TransformDirection(Vector3.down) * leftHit.distance, Color.red, debugLineLifetime);

            float leftHitAngle = Vector2.Angle(Vector2.up, new Vector2(leftHit.normal.x, leftHit.normal.y));

            // Check if Left point is over a slope
            if (leftHitAngle > 0.0f) {
                isAboveSlopeL = true;

                // Check if Left point is a over walkable slope
                if (leftHitAngle > maxSlopeAngle) {
                    isOnNonWalkableSlopeL = true;
                } else {
                    isOnNonWalkableSlopeL = false;
                }
            } else {
                isAboveSlopeL = false;
                isOnNonWalkableSlopeL = false;
            }

            bottomPositionL = leftHit.point.y;
            // Check if next movement would be passed the limits
            if (leftHit.point.y + groundOffsetY > nextBottomPosition || isOnSlope) {
                isGroundedL = true;

            } else {
                isGroundedL = false;
            }
        } else {
            Debug.DrawRay(bottomLeftCorner, transform.TransformDirection(Vector3.down) * probingMaxDistance, Color.white, debugLineLifetime);
            isGroundedL = false;
        }

        RaycastHit rightHit;
        if (Physics.Raycast(bottomRightCorner, transform.TransformDirection(Vector3.down), out rightHit, probingMaxDistance)) {
            Debug.DrawRay(bottomRightCorner, transform.TransformDirection(Vector3.down) * rightHit.distance, Color.red, debugLineLifetime);

            float rightHitAngle = Vector2.Angle(Vector2.up, new Vector2(rightHit.normal.x, rightHit.normal.y));

            // Check if Right point is over a slope
            if (rightHitAngle > 0.0f) {
                isAboveSlopeR = true;

                // Check if Right point is a over walkable slope
                if (rightHitAngle > maxSlopeAngle) {
                    isOnNonWalkableSlopeR = true;
                } else {
                    isOnNonWalkableSlopeR = false;
                }
            } else {
                isAboveSlopeR = false;
                isOnNonWalkableSlopeR = false;
            }

            bottomPositionR = rightHit.point.y;
            // Check if next movement would be passed the limits
            if (rightHit.point.y + groundOffsetY > nextBottomPosition || isOnSlope) {
                isGroundedR = true;

            } else {
                isGroundedR = false;
            }
        } else  {
            Debug.DrawRay(bottomRightCorner, transform.TransformDirection(Vector3.down) * probingMaxDistance, Color.white, debugLineLifetime);
            isGroundedR = false;
        }

        RaycastHit centerHit;
        if (Physics.Raycast(bottomCenterCorner, transform.TransformDirection(Vector3.down), out centerHit, probingMaxDistance)) {
            Debug.DrawRay(bottomCenterCorner, transform.TransformDirection(Vector3.down) * centerHit.distance, Color.red, debugLineLifetime);

            float centerHitAngle = Vector2.Angle(Vector2.up, new Vector2(centerHit.normal.x, centerHit.normal.y));

            // Check if Center point is over a slope
            if (centerHitAngle > 0.0f) {
                // Check if Center point is a over walkable slope
                if (centerHitAngle > maxSlopeAngle) {
                    isOnNonWalkableSlopeC = true;
                } else {
                    isOnNonWalkableSlopeC = false;
                }
            } else {
                isOnNonWalkableSlopeC = false;
            }

            bottomPositionC = centerHit.point.y;
            // Check if next movement would be passed the limits
            if (centerHit.point.y + groundOffsetY > nextBottomPosition || isOnSlope) {
                isGroundedC = true;

            } else {
                isGroundedC = false;
            }
        } else  {
            Debug.DrawRay(bottomCenterCorner, transform.TransformDirection(Vector3.down) * probingMaxDistance, Color.white, debugLineLifetime);
            isGroundedC = false;
        }

        isGrounded = isGroundedL || isGroundedR || isGroundedC;
        isOnSlope = isGrounded && isAboveSlope;
        isAboveSlope = (isAboveSlopeL && isAboveSlopeR)
            || (isAboveSlopeL && !isAboveSlopeR && bottomPositionL >= bottomPositionR)
            || (isAboveSlopeR && !isAboveSlopeL && bottomPositionR >= bottomPositionL);
        isOnNonWalkableSlope = isOnNonWalkableSlopeL || isOnNonWalkableSlopeR || isOnNonWalkableSlopeC;

        // Update Move Vector
        if (isOnSlope) {
            if (isAboveSlopeL && bottomPositionL - bottomPositionC > groundSlopeOffsetY) {
                nextBottomPosition = bottomPositionL;

            } else if (isAboveSlopeR && bottomPositionR - bottomPositionC > groundSlopeOffsetY) {
                nextBottomPosition = bottomPositionR;
                
            } else {
                nextBottomPosition = bottomPositionC;
            }

        } else {
            nextBottomPosition = Mathf.Max(nextBottomPosition, bottomPositionL, bottomPositionR, bottomPositionC);
        }
        
        moveVector.y = nextBottomPosition - bottomCenterCorner.y + rayBottomOffsetY;

        return moveVector;
    }

    private Vector3 ProbeUpward(Vector3 moveVector)
    {
        float nextTopPosition = topCenterCorner.y + moveVector.y + rayTopOffsetY;
        float topPositionR = nextTopPosition;
        float topPositionC = nextTopPosition;
        float topPositionL = nextTopPosition;

        RaycastHit leftHit;
        if (Physics.Raycast(topLeftCorner, transform.TransformDirection(Vector3.up), out leftHit, probingMaxDistance)) {
            Debug.DrawRay(topLeftCorner, transform.TransformDirection(Vector3.up) * leftHit.distance, Color.red, debugLineLifetime);

            topPositionR = leftHit.point.y;

            // Check if next movement would be passed the limits
            if (leftHit.point.y - groundOffsetY < nextTopPosition && leftHit.transform.gameObject.GetComponent<OneWayPlatform>() == null) {
                isBumpedTopL = true;

            } else {
                isBumpedTopL = false;
            }
        } else {
            Debug.DrawRay(topLeftCorner, transform.TransformDirection(Vector3.up) * probingMaxDistance, Color.white, debugLineLifetime);
            isBumpedTopL = false;
        }

        RaycastHit rightHit;
        if (Physics.Raycast(topRightCorner, transform.TransformDirection(Vector3.up), out rightHit, probingMaxDistance)) {
            Debug.DrawRay(topRightCorner, transform.TransformDirection(Vector3.up) * rightHit.distance, Color.red, debugLineLifetime);

            topPositionC = rightHit.point.y;

            // Check if next movement would be passed the limits
            if (rightHit.point.y - groundOffsetY < nextTopPosition  && rightHit.transform.gameObject.GetComponent<OneWayPlatform>() == null) {
                isBumpedTopR = true;

            } else {
                isBumpedTopR = false;
            }
        } else  {
            Debug.DrawRay(topRightCorner, transform.TransformDirection(Vector3.up) * probingMaxDistance, Color.white, debugLineLifetime);
            isBumpedTopR = false;
        }

        RaycastHit centerHit;
        if (Physics.Raycast(topCenterCorner, transform.TransformDirection(Vector3.up), out centerHit, probingMaxDistance)) {
            Debug.DrawRay(topCenterCorner, transform.TransformDirection(Vector3.up) * centerHit.distance, Color.red, debugLineLifetime);

            topPositionL = centerHit.point.y;

            // Check if next movement would be passed the limits
            if (centerHit.point.y - groundOffsetY < nextTopPosition  && centerHit.transform.gameObject.GetComponent<OneWayPlatform>() == null) {
                isBumpedTopC = true;

            } else {
                isBumpedTopC = false;
            }
        } else  {
            Debug.DrawRay(topCenterCorner, transform.TransformDirection(Vector3.up) * probingMaxDistance, Color.white, debugLineLifetime);
            isBumpedTopC = false;
        }

        isBumpedTop = isBumpedTopL || isBumpedTopR || isBumpedTopC;

        // Update Move Vector
        if (isBumpedTop) {
            // Set vertical Speed to 0.0f when Character bumps into a collision while jumping
            currentSpeedY = 0.0f;
            nextTopPosition = Mathf.Min(nextTopPosition, topPositionL, topPositionR, topPositionC);
            moveVector.y = nextTopPosition - topCenterCorner.y - rayTopOffsetY;
        }

        return moveVector;
    }

    private Vector3 ProbeSides(Vector3 moveVector)
    {
        float dir = Mathf.Sign(moveVector.x);
        float nextSidePosition = sideCenterCorner.x + moveVector.x + raySideOffsetX * dir;
        float sidePositionC = nextSidePosition;
        float sidePositionCheckC = nextSidePosition;
        float sidePositionT = nextSidePosition;
        float sidePositionCheckT = nextSidePosition;
        float sidePositionB = nextSidePosition;
        float sidePositionCheckB = nextSidePosition;
        float moveSign;

        RaycastHit topHit;
        if (Physics.Raycast(sideTopCorner, transform.TransformDirection(Vector3.right) * dir, out topHit, probingMaxDistance)) {
            float topHitAngle = Vector2.Angle(Vector2.right * dir, new Vector2(topHit.normal.x, topHit.normal.y)) - 90;
            Debug.DrawRay(sideTopCorner, transform.TransformDirection(Vector3.right) * dir * topHit.distance, Color.red, debugLineLifetime);
            
            sidePositionT = topHit.point.x;
            sidePositionCheckT = topHit.point.x - groundOffsetX * dir;
            moveSign = Mathf.Sign(nextSidePosition - sidePositionCheckT);

            // Check if next movement would be passed the limits or would be over an unwalkable slope
            if (moveSign == dir && topHitAngle > maxSlopeAngle) {
                isBumpedSideT = true;

            } else {
                isBumpedSideT = false;
            }
        } else {
            Debug.DrawRay(sideTopCorner, transform.TransformDirection(Vector3.right) * dir * probingMaxDistance, Color.white, debugLineLifetime);
            isBumpedSideT = false;
        }

        RaycastHit bottomHit;
        if (Physics.Raycast(sideBottomCorner, transform.TransformDirection(Vector3.right) * dir, out bottomHit, probingMaxDistance)) {
            float bottomHitAngle = Vector2.Angle(Vector2.right * dir, new Vector2(bottomHit.normal.x, bottomHit.normal.y)) - 90;
            Debug.DrawRay(sideBottomCorner, transform.TransformDirection(Vector3.right) * dir * bottomHit.distance, Color.red, debugLineLifetime);
            
            sidePositionB = bottomHit.point.x;
            sidePositionCheckB = bottomHit.point.x - groundOffsetX * dir;
            moveSign = Mathf.Sign(nextSidePosition - sidePositionCheckB);

            // Check if next movement would be passed the limits or would be over an unwalkable slope
            if (moveSign == dir && bottomHitAngle > maxSlopeAngle) {
                isBumpedSideB = true;

            } else {
                isBumpedSideB = false;
            }
        } else  {
            Debug.DrawRay(sideBottomCorner, transform.TransformDirection(Vector3.right) * dir * probingMaxDistance, Color.white, debugLineLifetime);
            isBumpedSideB = false;
        }

        RaycastHit centerHit;
        if (Physics.Raycast(sideCenterCorner, transform.TransformDirection(Vector3.right) * dir, out centerHit, probingMaxDistance)) {
            float centerHitAngle = Vector2.Angle(Vector2.right * dir, new Vector2(centerHit.normal.x, centerHit.normal.y)) - 90;
            Debug.DrawRay(sideCenterCorner, transform.TransformDirection(Vector3.right) * dir * centerHit.distance, Color.red, debugLineLifetime);
            
            sidePositionC = centerHit.point.x;
            sidePositionCheckC = centerHit.point.x - groundOffsetX * dir;
            moveSign = Mathf.Sign(nextSidePosition - sidePositionCheckC);

            // Check if next movement would be passed the limits or would be over an unwalkable slope
            if (moveSign == dir && centerHitAngle > maxSlopeAngle) {
                isBumpedSideC = true;

            } else {
                isBumpedSideC = false;
            }
        } else  {
            Debug.DrawRay(sideCenterCorner, transform.TransformDirection(Vector3.right) * dir * probingMaxDistance, Color.white, debugLineLifetime);
            isBumpedSideC = false;
        }

        isBumpedSide = (isBumpedSideT || isBumpedSideB || isBumpedSideC);

        // Update Move Vector
        if (isBumpedSide) {
            // Set horizontal Speed to 0.0f when Character bumps into a collision while running
            currentSpeedX = 0;

            if (!isOnNonWalkableSlope) {
                if (dir > 0) {
                    nextSidePosition = Mathf.Min(nextSidePosition, sidePositionT, sidePositionB, sidePositionC);

                } else {
                    nextSidePosition = Mathf.Max(nextSidePosition, sidePositionT, sidePositionB, sidePositionC);
                }
            } else {
                nextSidePosition = sideCenterCorner.x + raySideOffsetX * dir;
            }
        }

        moveVector.x = nextSidePosition - sideCenterCorner.x - raySideOffsetX * dir;
        
        return moveVector;
    }

    private void UpdateCornerPosition(Vector2 moveVector)
    {
        // Update 3 bottom probing points
        bottomLeftCorner = new Vector3(
            transform.position.x - colliderSize.x / 2 - rayBottomOffsetX,
            transform.position.y - colliderSize.y / 2 + rayBottomOffsetY
        );
        bottomRightCorner = new Vector3(
            transform.position.x + colliderSize.x / 2 + rayBottomOffsetX,
            transform.position.y - colliderSize.y / 2 + rayBottomOffsetY
        );
        bottomCenterCorner = new Vector3(
            transform.position.x,
            transform.position.y - colliderSize.y / 2 + rayBottomOffsetY
        );

        // Update 3 top probing points
        topLeftCorner = new Vector3(
            transform.position.x - colliderSize.x / 2 - rayBottomOffsetX,
            transform.position.y + colliderSize.y / 2 - rayBottomOffsetY
        );
        topRightCorner = new Vector3(
            transform.position.x + colliderSize.x / 2 + rayBottomOffsetX,
            transform.position.y + colliderSize.y / 2 - rayBottomOffsetY
        );
        topCenterCorner = new Vector3(
            transform.position.x,
            transform.position.y + colliderSize.y / 2 - rayBottomOffsetY
        );

        // Update 3 side probing points according to movement direction (dir)
        float dir = Mathf.Sign(moveVector.x);
        if (isRunning)
        {
            sideTopCorner = new Vector3(
                transform.position.x + (colliderSize.x / 2 - raySideOffsetX) * dir ,
                transform.position.y + colliderSize.y / 2
            );
            sideBottomCorner = new Vector3(
                transform.position.x + (colliderSize.x / 2 - raySideOffsetX) * dir ,
                transform.position.y - colliderSize.y / 2
            );
            sideCenterCorner = new Vector3(
                transform.position.x + (colliderSize.x / 2 - raySideOffsetX) * dir ,
                transform.position.y
            );
        }
    }
    #endregion
}
