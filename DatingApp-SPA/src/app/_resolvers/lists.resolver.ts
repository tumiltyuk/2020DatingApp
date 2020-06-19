import { Injectable } from '@angular/core';
import { User } from '../_models/user';
import { Resolve, Router, ActivatedRouteSnapshot } from '@angular/router';
import { UserService } from '../_services/user.service';
import { AlertifyService } from '../_services/alertify.service';
import { Observable, of } from 'rxjs';
import { catchError } from 'rxjs/operators';

@Injectable()
export class ListsResolver implements Resolve<User[]> {

    pageNumber = 1;
    pageSize = 5;
    likesParam = 'Likers';

    constructor(
        private userService: UserService,
        private router: Router,
        private alertify: AlertifyService
    ) {}

    resolve(route: ActivatedRouteSnapshot): Observable<User[]> {  // give method a route and return an observabe object of type User[]
        return this.userService.getUsers(this.pageNumber, this.pageSize, null, this.likesParam)

            .pipe(  // Catch error if a problem
                catchError(error => {
                    this.alertify.error('Problem retrieving data.');
                    this.router.navigate(['/home']);     // redirect to home
                    return of(null);
            })
        );
    }

}
