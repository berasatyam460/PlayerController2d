
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
  [Header("References")]
  public PlayerMovementStats moveStats;
  [SerializeField]private Collider2D feetCol;
  [SerializeField]private Collider2D bodyCol;


  private Rigidbody2D playerRB;

  //movement variables
  public  float horizontalVelocity{get;private set;}
  private bool isFacingRight;


  //checking collisions vars
  private RaycastHit2D groundHit;
  private RaycastHit2D headHit;

  private RaycastHit2D wallHit;
  private RaycastHit2D lastWallHit;

  public bool isGrounded;
  private bool bumpedHead;

  private bool isTouchingWall;


  //jump variables
  

  //jump Vars
  public float verticalVelocity{get;private set;}
  private bool isJumping;
  private bool isFastFalling;
  private bool isFalling;
  private float fastFallTime;
  private float fastFallReleaseSpeed;
  private int noOfJumpUsed;


  //apex vars
  private float apexPoint;
  private float timePAstApexThreshold;
  private bool isPastApexThreshold;

  //jump buffers vars
  private float jumpBufferTimer;
  private bool jumpReleasedDuringBuffer;

  //coyote Time vars
  private float coyoteTImer;


  //wall slide vars
  private bool isWallSliding;
  private bool isWallSlideFalling;


  //wall jump vars
  private bool useWallJumpMoveStats;
  private bool isWallJumping;
  private float wallJumpTime;
  private bool iswallJumpFastFalling;
  private float wallJumpFastFallTime;
  private float wallJumpFastFallReleaseSpeed;

  private float wallJumpPostBufferTimer;

  private float wallJumpApexPoint;
  private float timePastWallJumpApexThreshold;
  private bool isPastWallJumpApexThreshold;
  private bool isWallJumpFalling;
  

  //dash vars
  private bool isDashing;
  private bool isAirDashing;
  private float dashTimer;
  private float dashOnGroundTimer;
  private int noOfDashUsed;
  private Vector2 dashDirection;
  private bool isDashFastFalling;
  private float dashFastFallTime;
  private float dashFastFallReleaseTime;

  void Awake()
  {
    isFacingRight=true;

    playerRB=GetComponent<Rigidbody2D>();

  }
  private void Update() {
    CountTimers();
    JumpChecks();
    LandCheck();
    WallSlideCheck();
    WallJumpCheck();
    DashCheck();
  }

  private void FixedUpdate() {
    CollisionChecks();

    Jump();
    Fall();
    WallSlide();
    WallJump();
    Dash();

    if(isGrounded)
    {

      Move(moveStats.groundAcceleration,moveStats.groundDeceleration,InputManager.movement);
    
    }else
    {

      //wall jump
      if (useWallJumpMoveStats)
      {
        Move(moveStats.wallJumpMoveAccelaration,moveStats.wallJumpMoveDccelaration,InputManager.movement);

      }
      
      //airborne
      else{
        
        Move(moveStats.airAcceleration,moveStats.airDeceleration,InputManager.movement);
      }

    }
    ApplyVelocity();
  }
  private void ApplyVelocity(){
        //clamp fall speed

        if (!isDashing)
        {
          verticalVelocity = Mathf.Clamp(verticalVelocity, -moveStats.maxFallSpeed, 50f);

        }
        else
        {
          verticalVelocity = Mathf.Clamp(verticalVelocity, -50f, 50f);
            
        }
    
        playerRB.linearVelocity = new Vector2(horizontalVelocity, verticalVelocity);
  }
  #region Movement

  private void Move(float acceleration,float deceleration,Vector2 moveInput){

    if(!isDashing){

      if(Mathf.Abs(moveInput.x)>=moveStats.moveThreshold){
        //check if he needs to turn

        TurnCheck (moveInput);
        float targetVelocity=0f;
        //run and walk input
        if(InputManager.runIsHeld){
          targetVelocity=moveInput.x*moveStats.maxRunSpeed;
        }else{
          targetVelocity=moveInput.x*moveStats.maxWalkSpeed;
        }

        horizontalVelocity=Mathf.Lerp(horizontalVelocity,targetVelocity,acceleration*Time.fixedDeltaTime);
        

      }else if(Mathf.Abs(moveInput.x)<moveStats.moveThreshold){
        horizontalVelocity=Mathf.Lerp(horizontalVelocity,0f,deceleration*Time.fixedDeltaTime);
      
      }
    }
  }
#endregion

  #region Jump
  private void ResetJumpValues(){
    isJumping=false;
    isFalling=false;
    isFastFalling=false;
    fastFallTime=0f;
    isPastApexThreshold=false;

  }
  private void JumpChecks(){
    //when jump btn is pressed
    if(InputManager.jumpWasPressed){
      if(isWallSlideFalling && wallJumpPostBufferTimer >= 0f)
      {
        return;
      }


      else if(isWallSliding ||(isTouchingWall && !isGrounded))
      {
        return;
      }
      jumpBufferTimer=moveStats.jumpBufferTime;
      jumpReleasedDuringBuffer=false;
    }
    //when jump btn is released
    if(InputManager.jumpWasReleased){
      if(jumpBufferTimer>0f){
        jumpReleasedDuringBuffer=true;

      }
      if(isJumping&&verticalVelocity>0f){
        if(isPastApexThreshold){
          isPastApexThreshold=false;
          isFastFalling=true;
          fastFallTime=moveStats.timeForUpwardCancel;
          verticalVelocity=0f;
        }else{
          isFastFalling=true;
          fastFallReleaseSpeed=verticalVelocity;
        }
      }
    }

    //initiate jump with jump buffering and coyote time
    if(jumpBufferTimer>0f && !isJumping && (isGrounded||coyoteTImer>0f)){
        InitiateJump(1);

        if(jumpReleasedDuringBuffer){
           isFastFalling=true;
           fastFallReleaseSpeed=verticalVelocity;
        }
    }

    //double jump
    else if(jumpBufferTimer>0f  && (isJumping || isWallJumping || isWallSlideFalling || isAirDashing || isDashFastFalling ) && !isTouchingWall &&noOfJumpUsed<moveStats.noOfJumpAllowed){
      isFastFalling=false;
      InitiateJump(1);

            if (isDashFastFalling)
            {
                isDashFastFalling=false;

            }
    }
    //air jump after coyote time lapesed
    else if(jumpBufferTimer>0f && isFalling && !isWallSlideFalling && noOfJumpUsed<moveStats.noOfJumpAllowed-1){
      InitiateJump(2);
      isFastFalling=false;

    }
    
  }
 

  private void InitiateJump(int noOfJumpUsed){
    if(!isJumping){
      isJumping=true;
    }
    
    ResetWallJumpVAlues();
    jumpBufferTimer=0f;
    this.noOfJumpUsed+=noOfJumpUsed;
    verticalVelocity=moveStats.initialJumpVelocity;

  }
  private void Jump()
    {
        //apply gravity while jumping
        if (isJumping)
        {
            // check for head bump
            if (bumpedHead)
            {
                isFastFalling = true;

            }
            //gravity on ascending
            if (verticalVelocity >= 0f)
            {
                //apex control
                apexPoint = Mathf.InverseLerp(moveStats.initialJumpVelocity, 0f, verticalVelocity);

                if (apexPoint > moveStats.apexThreshold)
                {
                    if (!isPastApexThreshold)
                    {
                        isPastApexThreshold = true;
                        timePAstApexThreshold = 0f;

                    }
                    if(isPastApexThreshold)
                    {
                        timePAstApexThreshold += Time.deltaTime;
                        if (timePAstApexThreshold < moveStats.apexHangTime)
                        {
                            verticalVelocity = 0f;

                        }
                        else
                        {
                            verticalVelocity = -0.01f;
                        }
                    }
                }
                //gravity on ascending without past apex threshold
                else if(!isFastFalling)
                {
                    verticalVelocity += moveStats.gravity * Time.deltaTime;
                    if (isPastApexThreshold)
                    {
                        isPastApexThreshold = false;

                    }
                }

            }
            //gravity on descending
            else if (!isFastFalling)
            {
                verticalVelocity += moveStats.gravity * moveStats.gravityOnReleaseMultiplier * Time.deltaTime;
            }
            else if (verticalVelocity < 0f)
            {
                if (!isFalling)
                {
                    isFalling = true;
                }
            }


        }




        //jump cut
        if (isFastFalling)
        {
            if (fastFallTime >= moveStats.timeForUpwardCancel)
            {
                verticalVelocity += moveStats.gravity * moveStats.gravityOnReleaseMultiplier * Time.deltaTime;

            }else if (fastFallTime < moveStats.timeForUpwardCancel)
            {
                verticalVelocity = Mathf.Lerp(fastFallReleaseSpeed, 0f, (fastFallTime / moveStats.timeForUpwardCancel));

            }
            fastFallTime+= Time.deltaTime;
        }
        
    }

  #endregion

  #region Timers
  private void CountTimers(){
    jumpBufferTimer-=Time.deltaTime;
    if(!isGrounded){
      coyoteTImer-=Time.deltaTime;

    }else{
      coyoteTImer=moveStats.jumpCoyoteTime;

    }

    if (!ShouldApplyWallJumpBuffer())
    {
       wallJumpPostBufferTimer -=Time.deltaTime;
    }

    if (isGrounded)
    {
        dashOnGroundTimer -=Time.deltaTime;
    }
  }

  #endregion

#region TurnChecking
   private void TurnCheck(Vector2 moveInput)
    {
        if (isFacingRight && moveInput.x < 0)
        {
            Turn(false);
           // camerasmooth.callTurn();
        }else if(!isFacingRight && moveInput.x > 0)
        {
            Turn(true);
           // camerasmooth.callTurn();
        }
    }

    private void Turn(bool TurnRight)
    {
        if (TurnRight)
        {
            isFacingRight = true;
            transform.Rotate(0f, 180f, 0f);
        }
        else
        {
            isFacingRight = false;
            transform.Rotate(0f, -180f, 0f);
        }
       
    }
#endregion
//collision check
#region Collision Checks

private void CollisionChecks(){
  IsGroundedCheck();
  BumpedHead();
  IsTouchingWall();
}


public void IsGroundedCheck(){
  Vector2 boxCastOrigin=new Vector2(feetCol.bounds.center.x,feetCol.bounds.min.y);
  Vector2 boxCastSize=new Vector2(feetCol.bounds.size.x,moveStats.groundDetectionRayLength);
  
  //calculate
  groundHit=Physics2D.BoxCast(boxCastOrigin,boxCastSize,0f,Vector2.down,moveStats.groundDetectionRayLength,moveStats.GroundLayer);
  if(groundHit.collider!=null){
    isGrounded=true;

  }else{
    isGrounded=false;
  }

  #region Debug Visualization

  if(moveStats.debugShowIsGrounded){

    Color rayColor;

    if(isGrounded){

      rayColor=Color.green;

    }else{

      rayColor=Color.red;

    }
    
    Debug.DrawRay(new Vector2(boxCastOrigin.x-boxCastSize.x/2,boxCastOrigin.y),Vector2.down*moveStats.groundDetectionRayLength,rayColor);

    Debug.DrawRay(new Vector2(boxCastOrigin.x+boxCastSize.x/2,boxCastOrigin.y),Vector2.down*moveStats.groundDetectionRayLength,rayColor);
    
    Debug.DrawRay(new Vector2(boxCastOrigin.x-boxCastSize.x/2,boxCastOrigin.y-moveStats.groundDetectionRayLength),Vector2.right*boxCastSize.x,rayColor);
  }

  #endregion
}

private void BumpedHead(){
  Vector2 boxCastOrigins=new Vector2(feetCol.bounds.center.x,bodyCol.bounds.max.y);
  Vector2 boxCastSize=new Vector2(feetCol.bounds.size.x*moveStats.headWidth,moveStats.headDetectionRayLength);
  
 headHit=Physics2D.BoxCast(boxCastOrigins,boxCastSize,0f,Vector2.up,moveStats.headDetectionRayLength,moveStats.GroundLayer);
  if(headHit.collider!=null){
    bumpedHead=true;

  }else{
    bumpedHead=false;
  }
  #region Debug Visualization

  if(moveStats.debugShowHeadRays){
   float headWidth=moveStats.headWidth;
    Color rayColor;

    if(isGrounded){

      rayColor=Color.green;

    }else{

      rayColor=Color.red;

    }
    
    Debug.DrawRay(new Vector2(boxCastOrigins.x-boxCastSize.x/2*headWidth,boxCastOrigins.y),Vector2.up*moveStats.headDetectionRayLength,rayColor);

    Debug.DrawRay(new Vector2(boxCastOrigins.x+boxCastSize.x/2*headWidth,boxCastOrigins.y),Vector2.up*moveStats.headDetectionRayLength,rayColor);
    
    Debug.DrawRay(new Vector2(boxCastOrigins.x-boxCastSize.x/2*headWidth,boxCastOrigins.y-moveStats.headDetectionRayLength),Vector2.right*boxCastSize.x,rayColor);
  }
  #endregion
}
private void IsTouchingWall(){
  float originEndPoint=0f;
  if(isFacingRight){
    originEndPoint=bodyCol.bounds.max.x;
  }else{
    originEndPoint=bodyCol.bounds.min.x;
  }
  float adjustedHeight=bodyCol.bounds.size.y*moveStats.wallRayHeightMultipler;
  Vector2 boxCastOrigin=new Vector2(originEndPoint,bodyCol.bounds.center.y);
  Vector2 boxCastSize=new Vector2(moveStats.wallDetectionRayLength,adjustedHeight);
  //check with the wall layer
  wallHit=Physics2D.BoxCast(boxCastOrigin,boxCastSize,0f,transform.right,moveStats.wallDetectionRayLength,moveStats.wallLayer);
  if(wallHit.collider!=null){
    lastWallHit=wallHit;
    isTouchingWall=true;
  }else{
    isTouchingWall=false;
  }

  #region Debug Visualization
  if(moveStats.debugShowWallHit){
    Color rayColor;
    if(isTouchingWall){
      rayColor=Color.green;
    }else{
      rayColor=Color.red;
    }

    Vector2 boxBottomLeft=new Vector2(boxCastOrigin.x-boxCastSize.x/2,boxCastOrigin.y-boxCastSize.y/2);
    Vector2 boxBottomRight=new Vector2(boxCastOrigin.x+boxCastSize.x/2,boxCastOrigin.y-boxCastSize.y/2);
    Vector2 boxTopLeft=new Vector2(boxCastOrigin.x-boxCastSize.x/2,boxCastOrigin.y+boxCastSize.y/2);
    Vector2 boxTopRight=new Vector2(boxCastOrigin.x+boxCastSize.x/2,boxCastOrigin.y+boxCastSize.y/2);

    Debug.DrawLine(boxBottomLeft,boxBottomRight,rayColor);
    Debug.DrawLine(boxBottomRight,boxTopRight,rayColor);
    Debug.DrawLine(boxTopRight,boxTopLeft,rayColor);
    Debug.DrawLine(boxTopLeft,boxBottomLeft,rayColor);

  }

  #endregion
}
#endregion

 #region  LandCheck/Fall
  private void LandCheck(){
    if((isJumping||isFalling || isWallJumpFalling || isWallJumping || isWallSlideFalling || isWallSlideFalling || isDashFastFalling) && isGrounded && verticalVelocity<=0f){
      ResetJumpValues();
      StopWallSlide();
      ResetWallJumpVAlues();
      ResetDashes();

      noOfJumpUsed=0;


      verticalVelocity=Physics2D.gravity.y;

      if(isDashFastFalling && isGrounded)
      {
        ResestDashValues();
        return;

      }
        
      ResestDashValues();

    }
  }
  private void Fall(){
    //normal gravity while falling

        if(!isGrounded&& !isJumping &&!isWallSliding && !isDashing && !isDashFastFalling && !isWallJumping)
        {
            if (!isFalling)
                isFalling = true;
            verticalVelocity += moveStats.gravity * Time.deltaTime;
        }
  }
  #endregion


  #region Wall Slide
  private void WallSlideCheck(){
    if(isTouchingWall && !isGrounded &&!isDashing){
      if(verticalVelocity<0f && !isWallSliding){
        ResetJumpValues();
        ResetWallJumpVAlues();
        ResestDashValues();

        if(moveStats.resetDashOnWallSlide){
          ResetDashes();
        }
        isWallSlideFalling=false;
        isWallSliding=true;
        if(moveStats.resetJumpOnWallSlide){
          noOfJumpUsed=0;
        }
      }
    }else if(isWallSliding && !isTouchingWall && !isGrounded && !isWallSlideFalling){
      isWallSlideFalling=true;
      StopWallSlide();
    }
    else{
      StopWallSlide();
    }
  }
  private void StopWallSlide(){
    if(isWallSliding){
      //varies with different game
      //if player fall consume 1 jump only give 1 air jump
      noOfJumpUsed++;

      isWallSliding=false;
    }
  }
  private void WallSlide(){
    if(isWallSliding){
      verticalVelocity=Mathf.Lerp(verticalVelocity,-moveStats.wallSlideSpeed,moveStats.wallSlideDecelaration*Time.fixedDeltaTime);
    }
  }
  #endregion

  #region Wall Jump


  private void WallJumpCheck()
  {
        if (ShouldApplyWallJumpBuffer())
        {
            wallJumpPostBufferTimer =moveStats.wallJumpBufferTime;
        }

        //wall jump fast falling
        if(InputManager.jumpWasReleased && !isWallSliding && !isTouchingWall && isWallJumping)
        {
            if(verticalVelocity> 0f)
            {
                if (isPastWallJumpApexThreshold)
                {
                    isPastWallJumpApexThreshold=false;
                    iswallJumpFastFalling=true;
                    wallJumpFastFallTime =moveStats.timeForUpwardCancel;

                    verticalVelocity =0f;
                }
                else
                {
                    iswallJumpFastFalling =true;
                    wallJumpFastFallReleaseSpeed = verticalVelocity;

                }
            }
        }

        //actual jump with post wall buffer
        if(InputManager.jumpWasPressed && wallJumpPostBufferTimer > 0f)
        {
            InitiateWallJump();
            Debug.Log("Initiate Wall Jump");
        }

  }

    private void InitiateWallJump()
    {
        if (!isWallJumping)
        {
            isWallJumping=true;
            useWallJumpMoveStats =true;

        }
        StopWallSlide();
        ResetJumpValues();
        wallJumpTime =0f;

        verticalVelocity = moveStats.initialWallJumpVelocity;

        int dirMultiplier=0;
        Vector2 hitPoint = lastWallHit.collider.ClosestPoint(bodyCol.bounds.center);

        if (hitPoint.x >transform.position.x)
        {
            dirMultiplier =-1;

        }
        else
        {
            dirMultiplier=1;
        }

        horizontalVelocity =Mathf.Abs(moveStats.wallJumpDirection.x)*dirMultiplier;
    }
  private void WallJump()
  {

        //apply wall jump gravity 
        if (isWallJumping)
        {
            wallJumpTime+=Time.fixedDeltaTime;

            if(wallJumpTime >= moveStats.timeTillJumpApex)
            {
                useWallJumpMoveStats=false;
            }


            //hit Head
            if (bumpedHead)
            {
              iswallJumpFastFalling=true;
              useWallJumpMoveStats =false;
            }

            //gravity in acsending
            if(verticalVelocity >= 0f)
            {
                //apex control
                wallJumpApexPoint =Mathf.InverseLerp(moveStats.wallJumpDirection.y ,0f ,verticalVelocity);

                if(wallJumpApexPoint > moveStats.apexThreshold)
                {
                    if (!isPastWallJumpApexThreshold)
                    {
                        isPastWallJumpApexThreshold =true;
                        timePastWallJumpApexThreshold=0f;

                    }

                    if (isPastWallJumpApexThreshold)
                    {
                        timePastWallJumpApexThreshold +=Time.fixedDeltaTime;
                        if(timePastWallJumpApexThreshold < moveStats.apexHangTime)
                        {
                            verticalVelocity=0f;

                        }
                        else
                        {
                            verticalVelocity -=0.01f;
                        }
                    }
                }

                //gravity in ascending but not past apex threshold
                else if (!iswallJumpFastFalling)
                {
                    verticalVelocity +=moveStats.wallJumpGravity * Time.fixedDeltaTime;

                    if (isPastWallJumpApexThreshold)
                    {
                        isPastWallJumpApexThreshold=false;
                    }
                }
            }



            //gravity ion descending
            else if (!iswallJumpFastFalling)
            {
                verticalVelocity +=moveStats.wallJumpGravity *Time.fixedDeltaTime;
            }

            else if(verticalVelocity < 0f)
            {
                if (!isWallJumpFalling)
                {
                    isWallJumpFalling =true;
                }
            }
        }


        //handle wall jump cut time 
        if (iswallJumpFastFalling)
        {
            if(wallJumpFastFallTime >= moveStats.timeForUpwardCancel)
            {
                verticalVelocity +=moveStats.wallJumpGravity *moveStats.wallJumpGravityOnReleaseMultiplier *Time.fixedDeltaTime;
            }else if(wallJumpFastFallTime < moveStats.timeForUpwardCancel)
            {
                verticalVelocity =Mathf.Lerp(wallJumpFastFallReleaseSpeed ,0f,wallJumpFastFallTime/moveStats.timeForUpwardCancel);
            }

            wallJumpFastFallTime+=Time.fixedDeltaTime;
        }

  }

  private bool ShouldApplyWallJumpBuffer()
    {
      if(!isGrounded && (isTouchingWall || isWallSliding))
      {
          return true;
      }
      else
      {
        return false;      
      }
         
    }
  private void ResetWallJumpVAlues(){
    isWallSlideFalling=false;
    useWallJumpMoveStats=false;
    isWallJumping=false;
    iswallJumpFastFalling=false;
    isPastWallJumpApexThreshold=false;
    isWallJumpFalling=false;

    wallJumpFastFallTime=0f;
    wallJumpTime=0f;
  }
  #endregion

  #region Dash
  private void ResestDashValues(){
    isDashFastFalling=false;
    dashOnGroundTimer=-0.0f;
  }
  private void ResetDashes(){
    noOfDashUsed=0;
  }


  private void DashCheck()
  {
        if (InputManager.dashWasPressed)
        {
            //ground dash
            if(isGrounded && dashOnGroundTimer<0 && !isDashing)
            {
                InitiateDash();
            }
            //air dash
            else if (!isGrounded && !isDashing && noOfDashUsed < moveStats.noOfDashes)
            {
                isAirDashing=true;
                InitiateDash();

                if (wallJumpPostBufferTimer > 0f)
                {
                    noOfJumpUsed--;
                    if (noOfJumpUsed < 0)
                    {
                        noOfJumpUsed=0;
                    }
                }
            }
        }
  }

  private void InitiateDash()
    {
        dashDirection=InputManager.movement;

        Vector2 closestDirection=Vector2.zero;  
        float minDistance=Vector2.Distance(dashDirection,moveStats.dashDirections[0]);

        for(int i = 0; i < moveStats.dashDirections.Length; i++)
        {
            //skip if we hit it bang on 
            if(dashDirection == moveStats.dashDirections[i])
            {
                closestDirection=dashDirection;
                break;
            }

            float distance=Vector2.Distance(dashDirection,moveStats.dashDirections[i]);

          //check if this is a diagonal direction and apply bias
          bool isDiagonal=(Mathf.Abs(moveStats.dashDirections[i].x) == 1 && Mathf.Abs(moveStats.dashDirections[i].y)==1);
            if (isDiagonal)
            {
                distance -=moveStats.dashDiagonallyBias;
            }
            else if(distance < minDistance)
            {
                minDistance=distance;
                closestDirection=moveStats.dashDirections[i];
            }
        }


        //handle dir with no input 
        if(closestDirection == Vector2.zero)
        {
            if (isFacingRight)
            {
                closestDirection=Vector2.right;
            }
            else
            {
                closestDirection=Vector2.left;
            }
        }

        dashDirection=closestDirection;
        noOfDashUsed++;
        isDashing=true;
        dashTimer =0f;
        dashOnGroundTimer =moveStats.timeBtwDashOnGround;

        ResetJumpValues();
        ResetWallJumpVAlues();
        StopWallSlide();
    }
  private void Dash()
  {
        //stop the timer after the dash

        if (isDashing)
        {
          dashTimer +=Time.fixedDeltaTime;
          if(dashTimer >= moveStats.dashTime)
            {
                if (isGrounded)
                {
                    ResetDashes();

                }

                isAirDashing=false;
                isDashing=false;

                if(!isJumping && !isWallJumping)
                {
                    dashFastFallTime = 0f;
                    dashFastFallReleaseTime=verticalVelocity;


                    if (!isGrounded)
                    {
                        isDashFastFalling=true;

                    }
                }

                return;
            }


            horizontalVelocity=moveStats.dashSpeed * dashDirection.x;

            if(dashDirection.y !=0f || isAirDashing)
            {
                verticalVelocity =moveStats.dashSpeed *dashDirection.y;
            }
        }

        //handle dash cut
        else if (isDashFastFalling)
        {
            if(verticalVelocity > 0f)
            {
                if(dashFastFallTime < moveStats.dashTimeForUpwardCancel)
                {
                    verticalVelocity= Mathf.Lerp(dashFastFallReleaseTime,0f,dashFastFallTime/moveStats.dashTimeForUpwardCancel);
                }
                else if(dashFastFallTime >=moveStats.dashTimeForUpwardCancel)
                {
                    verticalVelocity +=moveStats.gravity * moveStats.dashGravityOnReleaseMultiplier *Time.fixedDeltaTime;
                }

                dashFastFallTime +=Time.fixedDeltaTime;
            }
            else
            {
                  verticalVelocity +=moveStats.gravity * moveStats.dashGravityOnReleaseMultiplier *Time.fixedDeltaTime;
            }
        }

  }

    #endregion


    void OnDrawGizmos()
    {
        if (moveStats.showWalkJumpArc)
        {
            DrawJumpArc(moveStats.maxWalkSpeed,Color.white);
        }

        if (moveStats.showRunJumpArc)
        {
            DrawJumpArc(moveStats.maxRunSpeed,Color.red);
        }
    }



    #region JumpVisualization
    private void DrawJumpArc(float moveSpeed, Color gizmoColor)
{
    Vector2 startPosition = new Vector2(feetCol.bounds.center.x, feetCol.bounds.min.y);
    Vector2 previousPosition = startPosition;
    float speed = 0f;
    if (moveStats.drawRight)
    {
        speed = moveSpeed;
    }
    else speed = -moveSpeed;
    Vector2 velocity = new Vector2(speed, moveStats.initialJumpVelocity);

    Gizmos.color = gizmoColor;

    float timeStep = 2 * moveStats.timeTillJumpApex / moveStats.arcResolutuion; // time step for the simulation
    // float totalTime = (2 * MoveStats.TimeTillJumpApex) + MoveStats.ApexHangTime; // total time of the arc including hang time

    for (int i = 0; i < moveStats.visualizationStep; i++)
    {
        float simulationTime = i * timeStep;
        Vector2 displacement;
        Vector2 drawPoint;

        if (simulationTime < moveStats.timeTillJumpApex) // Ascending
        {
            displacement = velocity * simulationTime + 0.5f * new Vector2(0, moveStats.gravity) * simulationTime * simulationTime;
        }
        else if (simulationTime < moveStats.timeTillJumpApex + moveStats.apexHangTime) // Apex hang time
        {
            float apexTime = simulationTime - moveStats.timeTillJumpApex;
            displacement = velocity * moveStats.timeTillJumpApex + 0.5f * new Vector2(0, moveStats.gravity) * moveStats.timeTillJumpApex * moveStats.timeTillJumpApex;
            displacement += new Vector2(speed, 0) * apexTime; // No vertical movement during hang time
        }
        else // Descending
        {
            float descendTime = simulationTime - (moveStats.timeTillJumpApex + moveStats.apexHangTime);
            displacement = velocity * moveStats.timeTillJumpApex + 0.5f * new Vector2(0, moveStats.gravity) * moveStats.timeTillJumpApex * moveStats.timeTillJumpApex;
            displacement += new Vector2(speed, 0) * moveStats.apexHangTime; // Horizontal movement during hang time
            displacement += new Vector2(speed, 0) * descendTime + 0.5f * new Vector2(0, moveStats.gravity) * descendTime * descendTime;
        }

        drawPoint = startPosition + displacement;

        if (moveStats.stopOnCollision)
        {
            RaycastHit2D hit = Physics2D.Raycast(previousPosition, drawPoint - previousPosition, Vector2.Distance(previousPosition, drawPoint), moveStats.GroundLayer);
            if (hit.collider != null)
            {
                // If a hit is detected, stop drawing the arc at the hit point
                Gizmos.DrawLine(previousPosition, hit.point);
                break;
            }

        }
        if (moveStats.stopOnCollision)
        {
            RaycastHit2D hit = Physics2D.Raycast(previousPosition, drawPoint - previousPosition, Vector2.Distance(previousPosition, drawPoint), moveStats.wallLayer);
            if (hit.collider != null)
            {
                // If a hit is detected, stop drawing the arc at the hit point
                Gizmos.DrawLine(previousPosition, hit.point);
                break;
            }

        }
        

        Gizmos.DrawLine(previousPosition, drawPoint);
        previousPosition = drawPoint;
    }
  }

  #endregion
  
  
}
    
    




    
    




