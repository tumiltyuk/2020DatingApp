import { Component, OnInit } from '@angular/core';
import { User } from 'src/app/_models/user';
import { BsModalRef, BsModalService } from 'ngx-bootstrap/modal';
import { AdminService } from 'src/app/_services/admin.service';
import { AlertifyService } from 'src/app/_services/alertify.service';
import { RolesModalComponent } from '../roles-modal/roles-modal.component';

@Component({
  selector: 'app-user-management',
  templateUrl: './user-management.component.html',
  styleUrls: ['./user-management.component.css']
})
export class UserManagementComponent implements OnInit {
  users: User[];
  bsModalRef: BsModalRef;

  constructor(private adminService: AdminService,
              private alertify: AlertifyService,
              private modalService: BsModalService) { }

  ngOnInit() {
    this.getUsersWithRoles();
  }

  getUsersWithRoles() {
    this.adminService.getUsersWithRoles().subscribe((users: User[]) => {
      this.users = users;
    }, error => {
      this.alertify.error(error);
    });
  }

  editRolesModal(user: User) {
    const initialState = {
      user,
      rolesToDisplay: this.getRolesArray(user)
    };
    this.bsModalRef = this.modalService.show(RolesModalComponent, {initialState});
    this.bsModalRef.content.updateSelectedRoles.subscribe((values) => {
      const rolesToUpdate = {
        roleNames: [...values         // "..." is spread feature spreads values into new array
                    .filter(el => el.checked === true) // return only "Checked" roles
                    .map(el => el.name)                // return role Names only
                   ]
      };

      if (rolesToUpdate) {
        this.adminService.updateUserRoles(user, rolesToUpdate).subscribe(() => {
          user.roles = [...rolesToUpdate.roleNames];
        }, error => {
          console.log(error);
        });
      }
    });
  }

  private getRolesArray(user) {
    const rolesToDisplay = [];
    const userRoles = user.roles;
    const availableRoles: any[] = [
      {name: 'Admin', value: 'Admin'},
      {name: 'Moderator', value: 'Moderator'},
      {name: 'Member', value: 'Member'},
      {name: 'VIP', value: 'VIP'}
    ];

    for (let i = 0; i < availableRoles.length; i++) {

      let isMatch = false;
      // If userRole and AvailableRole match then add "checked" property (TRUE) to availableRoles and then add availableRoles object to rolesToDisplay[]
      for (let j = 0; j < userRoles.length; j++) {
        if (availableRoles[i].name === userRoles[j]) {
          isMatch = true;
          availableRoles[i].checked = true;
          rolesToDisplay.push(availableRoles[i]);
          break;
        }
      }
      // If user role does not exist in available role then add "checked" property (FALSE), and add availableRole to rolesToDisplay[]
      if (!isMatch) {
        availableRoles[i].checked = false;
        rolesToDisplay.push(availableRoles[i]);
      }
    }

    return rolesToDisplay;
  }

}
