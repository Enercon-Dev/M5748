/**
 * Copyright (c) 2001-2004 Swedish Institute of Computer Science.
 * All rights reserved. 
 * 
 * Redistribution and use in source and binary forms, with or without modification, 
 * are permitted provided that the following conditions are met:
 *
 * 1. Redistributions of source code must retain the above copyright notice,
 *    this list of conditions and the following disclaimer.
 * 2. Redistributions in binary form must reproduce the above copyright notice,
 *    this list of conditions and the following disclaimer in the documentation
 *    and/or other materials provided with the distribution.
 * 3. The name of the author may not be used to endorse or promote products
 *    derived from this software without specific prior written permission. 
 *
 * THIS SOFTWARE IS PROVIDED BY THE AUTHOR ``AS IS'' AND ANY EXPRESS OR IMPLIED 
 * WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF 
 * MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT 
 * SHALL THE AUTHOR BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, 
 * EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT 
 * OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS 
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN 
 * CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING 
 * IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY 
 * OF SUCH DAMAGE.
 *
 * This file is part of and a contribution to the lwIP TCP/IP stack.
 *
 * Credits go to Adam Dunkels (and the current maintainers) of this software.
 *
 * Christiaan Simons rewrote this file to get a more stable echo application.
 *
 **/

 /* This file was modified by ST */

#include "general.h"
#include "tcp_echoserver.h"
#include "lwip/debug.h"
#include "lwip/stats.h"
#include "lwip/tcp.h"

#if LWIP_TCP

static struct tcp_pcb *tcp_echoserver_pcb; //used in tcp_echoserver_init only.
                                           // this is listening PCB - it probably may be used to stop listening for new connections

struct tcp_echoserver_struct es_struct[ES_MAX_PORTS];


static err_t tcp_echoserver_accept(void *arg, struct tcp_pcb *newpcb, err_t err);
static err_t tcp_echoserver_recv(void *arg, struct tcp_pcb *tpcb, struct pbuf *p, err_t err);
static void tcp_echoserver_error(void *arg, err_t err);
static err_t tcp_echoserver_poll(void *arg, struct tcp_pcb *tpcb);
static err_t tcp_echoserver_sent(void *arg, struct tcp_pcb *tpcb, u16_t len);
static void tcp_echoserver_send(struct tcp_pcb *tpcb, struct tcp_echoserver_struct *es);
static err_t tcp_echoserver_connection_close(struct tcp_pcb *tpcb, struct tcp_echoserver_struct *es, int bAbort);
static void clear_es(struct tcp_echoserver_struct *es);
static struct tcp_echoserver_struct* getNewConnStruct();


/**
  * @brief  Initializes the tcp echo server - called onced from Main
  * @param  None
  * @retval None
  */
void tcp_echoserver_init(void)
{
  //init es_struct
  int i;
  for (i=0; i<ES_MAX_PORTS; i++)
  {
    es_struct[i].state = ES_NONE;
    es_struct[i].inBufferOffset = 0;
    es_struct[i].pcb = NULL; //just in case
    es_struct[i].p = NULL; //just in case
    es_struct[i].pbuf_offset = 0;
    es_struct[i].seniority = 0;
  }

  /* create new tcp pcb */
  tcp_echoserver_pcb = tcp_new();

  if (tcp_echoserver_pcb != NULL)
  {
    err_t err;
    
    /* bind echo_pcb to port 5025 (SCPI protocol) */
    err = tcp_bind(tcp_echoserver_pcb, IP_ADDR_ANY, 5025);
    
    if (err == ERR_OK)
    {
      /* start tcp listening for echo_pcb */
      tcp_echoserver_pcb = tcp_listen(tcp_echoserver_pcb);
      
      /* initialize LwIP tcp_accept callback function */
      tcp_accept(tcp_echoserver_pcb, tcp_echoserver_accept);
    }
    else 
    {
      /* deallocate the pcb */
      memp_free(MEMP_TCP_PCB, tcp_echoserver_pcb);
    }
  }
}

/**
  * @brief  This function is the implementation of tcp_accept LwIP callback
  * @param  arg: not used
  * @param  newpcb: pointer on tcp_pcb struct for the newly created tcp connection
  * @param  err: not used 
  * @retval err_t: error status
  */
static err_t tcp_echoserver_accept(void *arg, struct tcp_pcb *newpcb, err_t err)
{
  struct tcp_echoserver_struct *es;

  LWIP_UNUSED_ARG(arg);
  LWIP_UNUSED_ARG(err);

  /* set priority for the newly accepted tcp connection newpcb */
  tcp_setprio(newpcb, TCP_PRIO_MIN);

  es = getNewConnStruct(); //get "conection state" structure for the new connection
  
  if (es->state != ES_NONE)
  {
    //an connection already opened
    //close(abort) it before accepting a new one
    //we are aborting the connection and not gracefully closing it for two reasons:
    //  1. We are droping the connection due to lack of recorses (max number of connections reached) so Abort is more sutable
    //  2. In case the old connection is dead (no pc on the other side) then just closing the connection is not enough as it's
    //     resourecs (packeges waiting to be transmitted) will not be released untill the other side closes the connection as well.
    // the return value MUST be ignored since we are closing onother connection, not the one that this function is called for
    tcp_echoserver_connection_close(es->pcb, es, TRUE);
  }
  
  es->state = ES_ACCEPTED;
  es->pcb = newpcb;
  es->p = NULL;
    
  /* pass newly allocated es structure as argument to newpcb */
  tcp_arg(newpcb, es);
    
  /* initialize lwip tcp_recv callback function for newpcb  */ 
  tcp_recv(newpcb, tcp_echoserver_recv);
    
  /* initialize lwip tcp_err callback function for newpcb  */
  tcp_err(newpcb, tcp_echoserver_error);
    
  /* initialize lwip tcp_poll callback function for newpcb */
  tcp_poll(newpcb, tcp_echoserver_poll, 0); //TODO - change the interval to 1
    
  return ERR_OK;  
}


/**
  * @brief  This function is the implementation for tcp_recv LwIP callback
  * @param  arg: pointer on a argument for the tcp_pcb connection
  * @param  tpcb: pointer on the tcp_pcb connection
  * @param  pbuf: pointer on the received pbuf
  * @param  err: error information regarding the reveived pbuf
  * @retval err_t: error code
  */
static err_t tcp_echoserver_recv(void *arg, struct tcp_pcb *tpcb, struct pbuf *p, err_t err)
{
  struct tcp_echoserver_struct *es;
  err_t ret_err;

  LWIP_ASSERT("arg != NULL",arg != NULL);
  
  if (arg == NULL)
  {
    tcp_abort(tpcb); //When calling this from one of the TCP callbacks, make sure you always return ERR_ABRT
    return ERR_ABRT;
  }
  
  es = (struct tcp_echoserver_struct *)arg;
  
  /* if we receive an empty tcp frame from client => close connection */
  if (p == NULL)
  {
    /* remote host closed connection */
    es->state = ES_CLOSING;
    if(es->p == NULL)
    {
       /* we're done, there is no incoming data waiting to be decoded */
       ret_err = tcp_echoserver_connection_close(tpcb, es, FALSE);
    }
    else
    {
      /* we're not done yet - there are incoming data waiting to be decoded */
      
      //TODO: check how the port is closed, after the data is decoded
      // else it will remain in a incorrect state
      ret_err = ERR_OK;
    }
    
  }   
  /* else : a non empty frame was received from client but for some reason err != ERR_OK */
  else if(err != ERR_OK)
  {
    /* free received pbuf*/
    if (p != NULL)
    {
      //es->p = NULL; //I think this was an echoserver bug
      pbuf_free(p);
    }
    ret_err = err;
  }
  else if(es->state == ES_ACCEPTED)
  {
    /* first data chunk in p->payload */
    es->state = ES_RECEIVED;
    
    /* store reference to incoming pbuf (chain) */
    es->p = p;
    
    /* initialize LwIP tcp_sent callback function */
    tcp_sent(tpcb, tcp_echoserver_sent); //TODO: is it needed?
    
    /* send back the received data (echo) */
    //tcp_echoserver_send(tpcb, es); //remaining from echoserver
    
    ret_err = ERR_OK;
  }
  else if (es->state == ES_RECEIVED)
  {
    /* more data received from client and previous data has been already sent*/
    if(es->p == NULL)
    {
      es->p = p;
  
      /* send back received data */
      //tcp_echoserver_send(tpcb, es); //remaining from echoserver
    }
    else
    {
      struct pbuf *ptr;

      /* chain pbufs to the end of what we recv'ed previously  */
      ptr = es->p;
      pbuf_cat(ptr,p);  //pbuf_chain was probably a bug
    }
    ret_err = ERR_OK;
  }
  else if(es->state == ES_CLOSING)
  {
    /* odd case, remote side closing twice, trash data */
    tcp_recved(tpcb, p->tot_len);
    //es->p = NULL;  //I think this was an echoserver bug
    pbuf_free(p);
    ret_err = ERR_OK;
  }
  else
  {
    /* unknown es->state, trash data  */
    tcp_recved(tpcb, p->tot_len);
    //es->p = NULL; //I think this was an echoserver bug
    pbuf_free(p);
    ret_err = ERR_OK;
  }
  return ret_err;
}

/**
  * @brief  This function implements the tcp_err callback function (called
  *         when a fatal tcp_connection error occurs. 
  * @param  arg: pointer on argument parameter 
  * @param  err: not used
  * @retval None
  */
static void tcp_echoserver_error(void *arg, err_t err)
{
  struct tcp_echoserver_struct *es;

  LWIP_UNUSED_ARG(err);

  if (arg != NULL)
  {
    es = (struct tcp_echoserver_struct *)arg;
    clear_es(es); //clear es structure
  }
}

/**
  * @brief  This function implements the tcp_poll LwIP callback function
  * @param  arg: pointer on argument passed to callback
  * @param  tpcb: pointer on the tcp_pcb for the current tcp connection
  * @retval err_t: error code
  */
static err_t tcp_echoserver_poll(void *arg, struct tcp_pcb *tpcb)
{
  err_t ret_err;
  struct tcp_echoserver_struct *es;

  es = (struct tcp_echoserver_struct *)arg;
  if (es != NULL)
  {
    if (es->p != NULL)
    {
      //tcp_sent(tpcb, tcp_echoserver_sent);
      /* there is a remaining pbuf (chain) , try to send data */
      //tcp_echoserver_send(tpcb, es); //remaining from echoserver 
    }
    else
    {
      /* no remaining pbuf (chain)  */
      if(es->state == ES_CLOSING)
      {
        /*  close tcp connection */
        return tcp_echoserver_connection_close(tpcb, es, FALSE);
      }
    }
    ret_err = ERR_OK;
  }
  else
  {
    /* nothing to be done - there is no es structure attaced to pcb*/
    tcp_abort(tpcb);
    ret_err = ERR_ABRT;
  }
  return ret_err;
}

/**
  * @brief  This function implements the tcp_sent LwIP callback (called when ACK
  *         is received from remote host for sent data) 
  * @param  None
  * @retval None
  */
static err_t tcp_echoserver_sent(void *arg, struct tcp_pcb *tpcb, u16_t len)
{
  struct tcp_echoserver_struct *es;

  LWIP_UNUSED_ARG(len);
  if (arg == NULL)
  {
    tcp_abort(tpcb); //When calling this from one of the TCP callbacks, make sure you always return ERR_ABRT
    return ERR_ABRT;
  }

  es = (struct tcp_echoserver_struct *)arg;
  if(es->p != NULL)
  {
    //there are incoming data waiting to be decoded
  }
  else
  {
    /* if no more data to decode and client closed connection*/
    if(es->state == ES_CLOSING)
      return tcp_echoserver_connection_close(tpcb, es, FALSE);
  }
  return ERR_OK;
}


/**
  * @brief  This function is used to send data for tcp connection
  * @param  tpcb: pointer on the tcp_pcb connection
  * @param  es: pointer on echo_state structure
  * @retval None
  */
static void tcp_echoserver_send(struct tcp_pcb *tpcb, struct tcp_echoserver_struct *es)
{
  struct pbuf *ptr;
  err_t wr_err = ERR_OK;
 
  return; //TODO: temove this function
  
  while ((wr_err == ERR_OK) &&
         (es->p != NULL) && 
         (es->p->len <= tcp_sndbuf(tpcb)))
  {
    
    /* get pointer on pbuf from es structure */
    ptr = es->p;

    /* enqueue data for transmission */
    wr_err = tcp_write(tpcb, ptr->payload, ptr->len, 1);
    
    if (wr_err == ERR_OK)
    {
      u16_t plen;
      u8_t freed;

      plen = ptr->len;
     
      /* continue with next pbuf in chain (if any) */
      es->p = ptr->next;
      
      if(es->p != NULL)
      {
        /* increment reference count for es->p */
        pbuf_ref(es->p);
      }
      
     /* chop first pbuf from chain */
      do
      {
        /* try hard to free pbuf */
        freed = pbuf_free(ptr);
      }
      while(freed == 0);
     /* we can read more data now */
     tcp_recved(tpcb, plen);
   }
   else if(wr_err == ERR_MEM)
   {
      /* we are low on memory, try later / harder, defer to poll */
     es->p = ptr;
   }
   else
   {
     /* other problem ?? */
   }
  }
}

/**
  * @brief  This functions closes the tcp connection
  * @param  tcp_pcb: pointer on the tcp connection
  * @param  es: pointer on echo_state structure
  * @param  bAbort: if TRUE the connection is aborted else gracefully closed 
  * @retval ERR_OK if connection has been closed successfully
  *         ERR_ABRT if the connection was forcefully closed (abborted)
  * if this function is called from from one of the TCP callbacks, ERR_ABRT must be returne back by the caller
  */
static err_t tcp_echoserver_connection_close(struct tcp_pcb *tpcb, struct tcp_echoserver_struct *es, int bAbort)
{
  
  /* remove all callbacks */
  tcp_arg(tpcb, NULL);
  tcp_sent(tpcb, NULL);
  tcp_recv(tpcb, NULL);
  tcp_err(tpcb, NULL);
  tcp_poll(tpcb, NULL, 0);
  
  //"disconnect" the es structure from the PCB. The reason is that even after our side of TCP conection is closed the
  //  other side may keep sending data (half close connection). If this happend we should detect it and abort the conection.
  tcp_arg(tpcb, NULL);  
  
  
  clear_es(es); //clear es structure
  // the PCB is actually closed and release in the next section
  
  /* close tcp connection */
  if (bAbort)
  {
    tcp_abort(tpcb); //When calling this from one of the TCP callbacks, make sure you always return ERR_ABRT
    return ERR_ABRT;
  }
  else if (tcp_close(tpcb) != ERR_OK)
  {
    //tcp failed to close - abort the connection
    tcp_abort(tpcb); //When calling this from one of the TCP callbacks, make sure you always return ERR_ABRT
    return ERR_ABRT;
  }
  
  return ERR_OK; 
}


static void clear_es(struct tcp_echoserver_struct *es)
{
  if (es == NULL)
    return;
  
  if (es->p != NULL)
  {
    //there are input PBUFs held by the es struct - release them
    pbuf_free(es->p); //TODO: verify that the entire pbuf chain is released
    es->p = NULL;
  }
  es->pbuf_offset = 0;
  es->state = ES_NONE;
  es->pcb = NULL; // the PCB should be released elseware
  es->inBufferOffset = 0;
}


static struct tcp_echoserver_struct* getNewConnStruct()
{
  struct tcp_echoserver_struct *es;
  
  es = &es_struct[0]; //Just so the seniority comparison won't fail
  
  int i;
  for (i=0; i<ES_MAX_PORTS; i++)
  {
    //search for empty struct (prefered) or for a struct with lowest seniority
    if (es_struct[i].state == ES_NONE)
    {
      es = &es_struct[i];  //an unused struct found;
      //es->seniority = 0;
      break;
    }
    else if (es_struct[i].seniority < es->seniority)
    {
      es = &es_struct[i]; //struct with lower seniority found
    }
  }
  
  //if (es->seniority < 0)
  //  es->seniority = 0; //Just in case, should never happend
  
  //decrease the seniority for all structs that has higher seniority then the one found
  for (i=0; i<ES_MAX_PORTS; i++)
  {
    if (es_struct[i].seniority > es->seniority)
      es_struct[i].seniority --;
  }
  
  es->seniority = ES_MAX_PORTS; //set the seniority of the found struct to the highest level 
  return es;
}


char* es_getLine(struct tcp_echoserver_struct *es)
{
  if (es == NULL)
  {
    return 0;
  }
  
  char ch=0;
  while (es->p != NULL)
  {
    if (es->pbuf_offset < es->p->len)
    {
      ch = ((uint8_t*)(es->p->payload))[es->pbuf_offset++];
      
    }
    if (es->pbuf_offset >= es->p->len)
    {
      struct pbuf *ptr;
      uint16_t plen;
      
      ptr = es->p;
      plen = ptr->len;
      /* continue with next pbuf in chain (if any) */
      es->p = ptr->next;
      
      if(es->p != NULL)
      {
        /* increment reference count for es->p */
        pbuf_ref(es->p);
      }
      
      /* chop first pbuf from chain */
      pbuf_free(ptr);

     /* we can read more data now */
     tcp_recved(es->pcb, plen);

     es->pbuf_offset = 0;
    }
    
    if (es->inBufferOffset >= ES_MAX_LINE_LENGTH-1)
      es->inBufferOffset = ES_MAX_LINE_LENGTH-1;
    
    es->inBuffer[es->inBufferOffset++] = ch; //save the new char
    
    if (ch == '\n' || ch == '\r')
    {
      es->inBuffer[es->inBufferOffset-1] = 0; //replace the new line char with zero
      if (es->inBufferOffset > 1)
      {
        //new line char found - return the string 
        es->inBufferOffset = 0; //prepare for next command
        return es->inBuffer; //the data in the buffer must be imidiately used as it will be overwritten by the next received line
      }
      else
      {
        //string contains only new line char - either an emply string or a leftover from previous \n\r new line siquence. ignore it.
        es->inBufferOffset = 0; //prepare for next command
      }
    }
    
  }
        
  return NULL;
}

#endif /* LWIP_TCP */
