import { Component, OnInit, Output, EventEmitter } from '@angular/core';
import { BsModalRef } from 'ngx-bootstrap/modal';
import { User } from 'src/app/_models/user';


@Component({
  selector: 'app-roles-modal',
  templateUrl: './roles-modal.component.html',
  styleUrls: ['./roles-modal.component.css']
})
export class RolesModalComponent implements OnInit {
  @Output() updateSelectedRoles = new EventEmitter();
  user: User;
  rolesToDisplay: any[];

  // Old variables
  // title: string;
  // list: any [] = [];
  // closeBtnName: string;
  // tru = true;

  constructor(public bsModalRef: BsModalRef) {}

  ngOnInit() {
  }

  updateRoles() {
    this.updateSelectedRoles.emit(this.rolesToDisplay);
    this.bsModalRef.hide();
  }

}