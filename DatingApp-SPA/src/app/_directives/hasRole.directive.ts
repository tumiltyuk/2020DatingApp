import { Directive, Input, ViewContainerRef, TemplateRef, OnInit } from '@angular/core';
import { AuthService } from '../_services/auth.service';

@Directive({
  selector: '[appHasRole]'
})
export class HasRoleDirective implements OnInit {
  @Input() appHasRole: string[];
  isVisable = false;

  constructor(private viewContainerRef: ViewContainerRef,
              private templateRef: TemplateRef<any>,
              private authService: AuthService) { }


  ngOnInit() {
    // Decide here weather to display or hide the template
    const userRoles = this.authService.decodedToken.role as Array<string>;

    // If there are no roles then clear the viewContainerRef
    if (!userRoles) {
      this.viewContainerRef.clear();
    }

    // Check if User have role needed to render element
    if (this.authService.roleMatch(this.appHasRole)) {
      if (!this.isVisable) {
        this.isVisable = true;
        this.viewContainerRef.createEmbeddedView(this.templateRef);
      } else {
        this.isVisable = false;
        this.viewContainerRef.clear();
      }
    }

  }

}
