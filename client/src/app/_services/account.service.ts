import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { isArray } from 'ngx-bootstrap/chronos';
import { map, ReplaySubject } from 'rxjs';
import { environment } from 'src/environments/environment';
import { User } from '../_models/User';
import { PresenceService } from './presence.service';

@Injectable({
  providedIn: 'root'
})
export class AccountService {
 baseUrl = environment.apiUrl;
  private currentUserSource = new ReplaySubject<User>(1);
  currentUser$ = this.currentUserSource.asObservable();

  constructor(private http: HttpClient, private presence: PresenceService) { }

  login(model:any)
  {
      return this.http.post(this.baseUrl + 'account/login',model).pipe(
        map((response:any) => {
          const user = response;
          if(user){
            this.setCurrentUser(user);
            this.presence.createHubConnection(user);
          }
        })
      )
  }

  register(model:any){
    return this.http.post(this.baseUrl + 'account/register',model).pipe(
      map((user:User) => {
        if(user)
        {
          this.setCurrentUser(user);
          this.presence.createHubConnection(user);
        }
        return user;
      }
    ))
  }

  setCurrentUser(user:User){
    user.roles=[];
    const roles = this.getDecodedToken(user.token).role;
    Array.isArray(roles) ? user.roles = roles : user.roles.push(roles);
    this.currentUserSource.next(user);
    localStorage.setItem('user',JSON.stringify(user));
  }
  logout()
  {
    localStorage.removeItem('user');
    this.currentUserSource.next(null);
    this.presence.stopHubConnection();
  }

  getDecodedToken(token){
    // if(token != null)
    return JSON.parse(atob(token.split('.')[1]));
  }
}
