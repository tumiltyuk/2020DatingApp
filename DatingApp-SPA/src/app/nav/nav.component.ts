import { Component, OnInit } from '@angular/core';
import { AuthService } from '../_services/auth.service';

@Component({
  selector: 'app-nav',
  templateUrl: './nav.component.html',
  styleUrls: ['./nav.component.css']
})
export class NavComponent implements OnInit {
  model: any = {};
  value: any = {};

  constructor(private authService: AuthService){}

  ngOnInit() {
  }

  login() {
    this.authService.login(this.model).subscribe(
       next => {
        console.log('Logged in successfully');
       },
      error => {
        console.log('Unauthorised - Computer says No');
      });
  }

  loggedIn(){
    const token = localStorage.getItem('token');
    return !!token; // If token exists then return token.
  }

  logout(){
    localStorage.removeItem('token');
    console.log('Logged Out');
  }
}
