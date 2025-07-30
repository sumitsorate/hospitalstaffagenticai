import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

@Component({
  selector: 'app-header',
  templateUrl: './header.html',
  imports: [CommonModule, FormsModule],
})
export class Header {
  constructor(private router: Router, private auth: AuthService) { }

  onLogout() {
    this.auth.logout().subscribe({
      next: (res: any) => {
        localStorage.clear(); // remove token or any stored user info
        this.router.navigate(['/login']);
      },
      error: (err) => {
        console.warn('Logout failed or already logged out', err);
        // Still clear local storage and navigate to login
        localStorage.clear();
        this.router.navigate(['/login']);
      }
    });
  }
}
